# https://huggingface.co/google/gemma-3-4b-it
# pip install git+https://github.com/huggingface/transformers@v4.49.0-Gemma-3
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install accelerate

# huggingface-cli login --token hf_...


#from transformers import AutoProcessor, AutoModelForImageTextToText
#from transformers import AutoProcessor, Gemma3ForConditionalGeneration
from transformers import pipeline
from PIL import Image
#import requests
import torch
import time
import my_secret

import os
import subprocess

'''
$ which huggingface-cli
/home/takumi/.local/bin/huggingface-cli
'''
huggingface_bin_path = "/home/takumi/.local/bin"
os.environ["PATH"] = f"{huggingface_bin_path}:{os.environ['PATH']}"
#subprocess.run(["huggingface-cli", "login", "--token", my_secret.hf_token], shell=True)
subprocess.run(["huggingface-cli", "login", "--token", my_secret.hf_token])

model_id = "google/gemma-3-4b-it"
device = "cuda"


# pipeline
pipe = pipeline(
    "image-text-to-text",
    model=model_id,
    device="cuda",
    torch_dtype=torch.bfloat16
)


'''
# Gemma3ForConditionalGeneration
model = Gemma3ForConditionalGeneration.from_pretrained(
    model_id, device_map="auto"
).eval()

processor = AutoProcessor.from_pretrained(model_id)
'''

'''
# Load model directly
# AutoModelForImageTextToText
processor = AutoProcessor.from_pretrained(model_id)
model = AutoModelForImageTextToText.from_pretrained(model_id, token=my_secret.hf_token).to(device).eval()
'''

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

'''
inputs = processor.apply_chat_template(
    messages, add_generation_prompt=True, tokenize=True,
    return_dict=True, return_tensors="pt"
).to(model.device, dtype=torch.bfloat16)

input_len = inputs["input_ids"].shape[-1]

with torch.inference_mode():
    generation = model.generate(**inputs, max_new_tokens=500, do_sample=False)
    generation = generation[0][input_len:]

decoded = processor.decode(generation, skip_special_tokens=True)
print(decoded)
'''

# pipeline
output = pipe(text=messages, max_new_tokens=500)
print(output[0]["generated_text"][-1]["content"])


t_end = time.localtime()

print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)
