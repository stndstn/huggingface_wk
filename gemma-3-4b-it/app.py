# https://huggingface.co/google/gemma-3-4b-it
# pip install git+https://github.com/huggingface/transformers@v4.49.0-Gemma-3
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install accelerate

# huggingface-cli login --token hf_...


from transformers import pipeline
import torch
#from PIL import Image


from transformers import AutoProcessor, Gemma3ForConditionalGeneration
from PIL import Image
#import requests
import torch
import time

hf_token = "hf_..."
model_id = "google/gemma-3-12b-it"
device = "cuda"

model = Gemma3ForConditionalGeneration.from_pretrained(model_id).to(device).eval()

processor = AutoProcessor.from_pretrained(model_id)

image = Image.open("../images/CSDEMOBANK_ApplicationForm_P1_s.jpeg")

# You are a helpful assistant. Please read this bank account application form and extract information of applicant.
messages = [
    {
        "role": "system",
        "content": [{"type": "text", "text": "You are a helpful assistant."}]
    },
    {
        "role": "user",
        "content": [
            #{"type": "image", "image": "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/bee.jpg"},
            #{"type": "text", "text": "Describe this image in detail."}
            {"type": "image", "image": image},
            {"type": "text", "text": "You are a helpful assistant. Please read this bank account application form and extract information of applicant."}
        ]
    }
]

t_start = time.localtime()

inputs = processor.apply_chat_template(
    messages, add_generation_prompt=True, tokenize=True,
    return_dict=True, return_tensors="pt"
).to(model.device, dtype=torch.bfloat16)

input_len = inputs["input_ids"].shape[-1]

with torch.inference_mode():
    generation = model.generate(**inputs, max_new_tokens=100, do_sample=False)
    generation = generation[0][input_len:]

decoded = processor.decode(generation, skip_special_tokens=True)

t_end = time.localtime()

print(decoded)

print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)
