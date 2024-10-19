# https://huggingface.co/blog/finetune-florence2

# pip install einops timm
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install "numpy<2.0"
# pip install psutil
# (python -m pip install wheel) 
# pip install flash-attn --no-build-isolation
# pip install transformers
'''
Florence2LanguageForConditionalGeneration has generative capabilities, as prepare_inputs_for_generation is explicitly overwritten. 
However, it doesn't directly inherit from GenerationMixin. 
From 👉v4.50👈 onwards, PreTrainedModel will NOT inherit from GenerationMixin, and this model will lose the ability to call generate and other related functions.
If you're using trust_remote_code=True, you can get rid of this warning by loading the model with an auto class. See https://huggingface.co/docs/transformers/en/model_doc/auto#auto-classes
If you are the owner of the model architecture code, please modify your model class such that it inherits from GenerationMixin (after PreTrainedModel, otherwise you'll get an exception).
'''
# (install rust compiler, then 'pip install "transformers==4.44.2"'

# pip install -q datasets flash_attn timm einops

import os 
import torch
from datasets import load_dataset 
from transformers import AutoModelForCausalLM, AutoProcessor
from torch.utils.data import Dataset 
from torch.utils.data import DataLoader
from tqdm import tqdm 
from transformers import AdamW, get_scheduler



class DocVQADataset(Dataset): 

    def __init__(self, data): 
        self.data = data
        
    def __len__(self): 
        return len(self.data)
        
    def __getitem__(self, idx):
        example = self.data[idx]
        question = "<DocVQA>" + example['question'] 
        first_answer = example['answers'][0]
        image = example['image'].convert("RGB")
        return question, first_answer, image
    


def collate_fn(batch): 
    questions, answers, images = zip(*batch)
    inputs = processor(text=list(questions), images=list(images), return_tensors="pt", padding=True).to(device)
    return inputs, answers 



data = load_dataset("HuggingFaceM4/DocumentVQA")

device = torch.device("cuda" if torch.cuda.is_available() else "cpu") 

model = AutoModelForCausalLM.from_pretrained(
    "microsoft/Florence-2-base-ft",
    trust_remote_code=True,
    revision='refs/pr/6'
).to(device) 
processor = AutoProcessor.from_pretrained("microsoft/Florence-2-base-ft", 
    trust_remote_code=True, revision='refs/pr/6')

for param in model.vision_tower.parameters():
  param.is_trainable = False

train_dataset = DocVQADataset(data['train'])
val_dataset = DocVQADataset(data['validation']) 
#batch_size = 6
batch_size = 1
num_workers = 0

train_loader = DataLoader(train_dataset, batch_size=batch_size, 
                          collate_fn=collate_fn, num_workers=num_workers, shuffle=True)
val_loader = DataLoader(val_dataset, batch_size=batch_size, 
                          collate_fn=collate_fn, num_workers=num_workers)

epochs = 7
optimizer = AdamW(model.parameters(), lr=1e-6)
num_training_steps = epochs * len(train_loader)

lr_scheduler = get_scheduler(name="linear", optimizer=optimizer, 
                              num_warmup_steps=0, num_training_steps=num_training_steps,)

for epoch in range(epochs): 
    model.train() 
    train_loss = 0
    i = -1
    for inputs, answers in tqdm(train_loader, desc=f"Training Epoch {epoch + 1}/{epochs}"):
        i += 1
        input_ids = inputs["input_ids"]
        pixel_values = inputs["pixel_values"] 
        labels = processor.tokenizer(text=answers, return_tensors="pt", padding=True, return_token_type_ids=False).input_ids.to(device)
        outputs = model(input_ids=input_ids, pixel_values=pixel_values, labels=labels)
        loss = outputs.loss
        loss.backward()
        optimizer.step()
        lr_scheduler.step()
        optimizer.zero_grad()
        train_loss += loss.item()
    avg_train_loss = train_loss / len(train_loader)
    print(f"Average Training Loss: {avg_train_loss}")

    model.eval()
    val_loss = 0
    with torch.no_grad():
        for batch in tqdm(val_loader, desc=f"Validation Epoch {epoch + 1}/{epochs}"):
            inputs, answers = batch
            input_ids = inputs["input_ids"]
            pixel_values = inputs["pixel_values"]
            labels = processor.tokenizer(text=answers, return_tensors="pt", padding=True, return_token_type_ids=False).input_ids.to(device)
            outputs = model(input_ids=input_ids, pixel_values=pixel_values, labels=labels)
            loss = outputs.loss
            val_loss += loss.item()

    print(val_loss / len(val_loader))
    
    # Save model checkpoint
    output_dir = f"./model_checkpoints/epoch_{epoch+1}"
    os.makedirs(output_dir, exist_ok=True)
    model.save_pretrained(output_dir)
    processor.save_pretrained(output_dir)
  
    
print("completed!")
  