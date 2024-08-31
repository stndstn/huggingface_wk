# pip install einops timm
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install psutil
# pip install flash-attn --no-build-isolation
import requests
import torch

from PIL import Image
from transformers import AutoProcessor, AutoModelForCausalLM 


device = "cuda:0" if torch.cuda.is_available() else "cpu"
torch_dtype = torch.float16 if torch.cuda.is_available() else torch.float32

model = AutoModelForCausalLM.from_pretrained("microsoft/Florence-2-base", torch_dtype=torch_dtype, trust_remote_code=True).to(device)
processor = AutoProcessor.from_pretrained("microsoft/Florence-2-base", trust_remote_code=True)

# prompt = "<OD>"
#task = "<OD>"
prompt = "<OCR>"
task = "<OCR>"

# url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/car.jpg?download=true"
# image = Image.open(requests.get(url, stream=True).raw)
# image = Image.open("..\\..\\images\\MYDL1_s.jpg")
image = Image.open("..\\images\\handwritten1.jpg")
# image = Image.open("..\\images\\CSDEMOBANK.jpg")

inputs = processor(text=prompt, images=image, return_tensors="pt").to(device, torch_dtype)

generated_ids = model.generate(
    input_ids=inputs["input_ids"],
    pixel_values=inputs["pixel_values"],
    max_new_tokens=1024,
    do_sample=False,
    num_beams=3,
)
generated_text = processor.batch_decode(generated_ids, skip_special_tokens=False)[0]

parsed_answer = processor.post_process_generation(generated_text, task, image_size=(image.width, image.height))

print(parsed_answer)
