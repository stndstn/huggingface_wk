# pip install Pillow requests torch transformers
# pip install pytesseract
from transformers import pipeline, AutoProcessor
from PIL import Image    
import requests
import torch
import pytesseract
import time

print("start: ", time.strftime("%H:%M:%S", time.localtime()))

model_id = "llava-hf/llava-1.5-7b-hf"

print("calling AutoProcessor.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))

processor = AutoProcessor.from_pretrained("llava-hf/llava-1.5-7b-hf", trust_remote_code=True)
device_id = "cuda:0" if torch.cuda.is_available() else "cpu"

print("calling pipeline...", time.strftime("%H:%M:%S", time.localtime()))

# pipe = pipeline("image-to-text", model=model_id, device=device_id)
pipe = pipeline(
    "image-to-text",
    model=model_id,
    device=device_id,
)
#url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/ai2d-demo.jpg"
#image = Image.open(requests.get(url, stream=True).raw)

# Define a chat histiry and use `apply_chat_template` to get correctly formatted prompt
# Each value in "content" has to be a list of dicts with types ("text", "image") 
'''
conversation = [
    {
      "role": "user",
      "content": [
          {"type": "text", "text": "What does the label 15 represent? (1) lava (2) core (3) tunnel (4) ash cloud"},
          {"type": "image"},
        ],
    },
]
'''
# image = Image.open("..\\images\\CSDEMOBANK.jpg")
image = Image.open("..\\images\\MYDL2.png")
conversation = [
    {
      "role": "user",
      "content": [
          #{"type": "text", "text": "What is the title of this form?"},
          #{"type": "text", "text": "What is the text filled in the box labeled 'Name (Last, Suffix, First, Middle)' in 'SECTION A PERSONAL INFORMATION'?"},
          #{"type": "text", "text": "List all the information in 'SECTION A PERSONAL INFORMATION' of this form."},
          {"type": "text", "text": "List all the text in this document."},
          #{"type": "text", "text": "List all the information in this image."},
          {"type": "image"},
        ],
    },
]

print("calling processor.apply_chat_template...", time.strftime("%H:%M:%S", time.localtime()))
# prompt = processor.apply_chat_template(conversation, add_generation_prompt=True)
prompt = processor.apply_chat_template(conversation, add_generation_prompt=False)

print("calling pipe...", time.strftime("%H:%M:%S", time.localtime()))
outputs = pipe(image, prompt=prompt, generate_kwargs={"max_new_tokens": 500})

print("finish: ", time.strftime("%H:%M:%S", time.localtime()))
print(outputs)
