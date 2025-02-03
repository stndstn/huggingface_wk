# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# (python -m pip install wheel) 
# pip install flash-attn --no-build-isolation
# pip install transformers

import torch
from transformers import AutoProcessor, AutoModelForVision2Seq
from PIL import Image

#DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
DEVICE = "cpu"
print(f"DEVICE: {DEVICE}")

image = Image.open("..\\images\\MYDL1_s.jpg")

# Initialize processor and model
processor = AutoProcessor.from_pretrained("HuggingFaceTB/SmolVLM-500M-Instruct")
print(f"processor: {processor}")

model = AutoModelForVision2Seq.from_pretrained(
    "HuggingFaceTB/SmolVLM-500M-Instruct",
    torch_dtype=torch.bfloat16,
    _attn_implementation="flash_attention_2" if DEVICE == "cuda" else "eager",
)
print(f"model: {model}")

# Create input messages
messages = [
    {
        "role": "user",
        "content": [
            {"type": "image"},
#            {"type": "text", "text": "Can you describe this image?"}
            {"type": "text", "text": "What is the name of license holder?"}
        ]
    },
]

# Preprocess
prompt = processor.apply_chat_template(messages, add_generation_prompt=True)
print(f"prompt: {prompt}")
inputs = processor(text=prompt, images=[image], return_tensors="pt")
print(f"inputs: {inputs}")

# Generate
generated_ids = model.generate(**inputs, max_new_tokens=500)
print(f"generated_ids: {generated_ids}")
generated_texts = processor.batch_decode(
    generated_ids,
    skip_special_tokens=True,
)
print(f"generated_texts: {generated_texts}")
