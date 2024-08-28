# pip install Pillow requests torch transformers
from transformers import pipeline, AutoProcessor
from PIL import Image    
import requests
import torch

model_id = "llava-hf/llava-1.5-7b-hf"
processor = AutoProcessor.from_pretrained("llava-hf/llava-1.5-7b-hf", trust_remote_code=True)
device_id = "cuda:0" if torch.cuda.is_available() else "cpu"
pipe = pipeline("image-to-text", model=model_id, device=device_id)
url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/ai2d-demo.jpg"
image = Image.open(requests.get(url, stream=True).raw)

# Define a chat histiry and use `apply_chat_template` to get correctly formatted prompt
# Each value in "content" has to be a list of dicts with types ("text", "image") 
conversation = [
    {

      "role": "user",
      "content": [
          {"type": "text", "text": "What does the label 15 represent? (1) lava (2) core (3) tunnel (4) ash cloud"},
          {"type": "image"},
        ],
    },
]
prompt = processor.apply_chat_template(conversation, add_generation_prompt=True)

outputs = pipe(image, prompt=prompt, generate_kwargs={"max_new_tokens": 200})
print(outputs)
