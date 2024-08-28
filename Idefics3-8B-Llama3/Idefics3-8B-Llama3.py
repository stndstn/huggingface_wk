# pip install requests Pillow
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124

import requests
import torch
from PIL import Image
from io import BytesIO
import time

# https://huggingface.co/HuggingFaceM4/Idefics3-8B-Llama3/discussions/1
# https://github.com/huggingface/transformers/pull/32473
# https://github.com/andimarafioti/transformers/tree/idefics3
# pip install git+https://github.com/andimarafioti/transformers.git@idefics3
from transformers import AutoProcessor, AutoModelForVision2Seq
from transformers.image_utils import load_image

# print time hh:mm:ss
now = time.localtime()
print('start ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

DEVICE = "cuda:0"

# Note that passing the image urls (instead of the actual pil images) to the processor is also possible
#image1 = load_image("https://cdn.britannica.com/61/93061-050-99147DCE/Statue-of-Liberty-Island-New-York-Bay.jpg")
#image2 = load_image("https://cdn.britannica.com/59/94459-050-DBA42467/Skyline-Chicago.jpg")
#image3 = load_image("https://cdn.britannica.com/68/170868-050-8DDE8263/Golden-Gate-Bridge-San-Francisco.jpg")
image = Image.open("images/MYDL1_s.jpg")

now = time.localtime()
print('calling AutoProcessor.from_pretrained... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

processor = AutoProcessor.from_pretrained("HuggingFaceM4/Idefics3-8B-Llama3")

now = time.localtime()
print('calling AutoModelForVision2Seq.from_pretrained... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

model = AutoModelForVision2Seq.from_pretrained(
    "HuggingFaceM4/Idefics3-8B-Llama3", torch_dtype=torch.bfloat16
).to(DEVICE)

# Create inputs
'''
messages = [
    {
        "role": "user",
        "content": [
            {"type": "image"},
            {"type": "text", "text": "What do we see in this image?"},
        ]
    },
    {
        "role": "assistant",
        "content": [
            {"type": "text", "text": "In this image, we can see the city of New York, and more specifically the Statue of Liberty."},
        ]
    },
    {
        "role": "user",
        "content": [
            {"type": "image"},
            {"type": "text", "text": "And how about this image?"},
        ]
    },       
]
'''
messages = [
    {
        "role": "user",
        "content": [
            {"type": "image"},
            {"type": "text", "text": "This is Mayaisian driving license. What is the name of license holder?"},
        ]
    },
]

now = time.localtime()
print('calling processor.apply_chat_template... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

prompt = processor.apply_chat_template(messages, add_generation_prompt=True)

now = time.localtime()
print('calling processor()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

inputs = processor(text=prompt, images=[image], return_tensors="pt")
inputs = {k: v.to(DEVICE) for k, v in inputs.items()}


# Generate
now = time.localtime()
print('calling model.generate()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

generated_ids = model.generate(**inputs, max_new_tokens=500)

now = time.localtime()
print('calling processor.batch_decode()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

generated_texts = processor.batch_decode(generated_ids, skip_special_tokens=True)

now = time.localtime()
print('finished ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

print(generated_texts)