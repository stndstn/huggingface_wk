# https://huggingface.co/microsoft/udop-large
# https://github.com/NielsRogge/Transformers-Tutorials/blob/master/UDOP/Inference_with_UDOP%2C_a_generative_document_AI_model.ipynb
# pip install -q git+https://github.com/huggingface/transformers.git
# pip install "numpy<2.0"
# pip install -q sentencepiece
# 'sudo apt install tesseract-ocr' or tesseract-ocr-w64-setup-5.4.0.20240606.exe
# pip install -q pytesseract
# 'tesseract --version' or open cmd and '"C:\Program Files\Tesseract-OCR\tesseract.exe" --version'
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# https://github.com/protocolbuffers/protobuf/tree/master/python#installation
# pip install protobuf

from transformers import UdopProcessor, UdopForConditionalGeneration
from huggingface_hub import hf_hub_download
from PIL import Image
import pytesseract
import torch
import time

# https://stackoverflow.com/questions/50655738/how-do-i-resolve-a-tesseractnotfounderror
# https://pypi.org/project/pytesseract/
pytesseract.pytesseract.tesseract_cmd = "C:\\Program Files\\Tesseract-OCR\\tesseract.exe"

# Load model and processor
repo_id = "microsoft/udop-large"

# print time as hh:mm:ss
print("start: ", time.strftime("%H:%M:%S", time.localtime()))

print("calling UdopProcessor.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))

processor = UdopProcessor.from_pretrained(repo_id)

print("calling UdopForConditionalGeneration.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))

model = UdopForConditionalGeneration.from_pretrained(repo_id)

# Load image
#filepath = hf_hub_download(
#        repo_id="hf-internal-testing/fixtures_docvqa", filename="document_2.png", repo_type="dataset"
#    )
#image = Image.open(filepath).convert("RGB")
#image = Image.open("..\\..\\images\\document_2.png")
image = Image.open("..\\images\\CSDEMOBANK.jpg")

#width, height = image.size
#display(image.resize((int(0.3*width), (int(0.3*height)))))


# Prepare for the model
# prompt = "Question answering. In which year is the report made?"
# prompt = "document classification."
# prompt = "Question answering. What is the title of the presentation?"
prompt = "Question answering. What is the title of this form?"
print("calling processor...", time.strftime("%H:%M:%S", time.localtime()))

encoding = processor(images=image, text=prompt, return_tensors="pt")
for k,v in encoding.items():
  print(k, v.shape)

print("calling processor.batch_decode...", time.strftime("%H:%M:%S", time.localtime()))

processor.batch_decode(encoding.input_ids)

print("calling model.generate...", time.strftime("%H:%M:%S", time.localtime()))

# Generate
outputs = model.generate(**encoding, max_new_tokens=20)     

print("calling processor.batch_decode...", time.strftime("%H:%M:%S", time.localtime()))

generated_text = processor.batch_decode(outputs, skip_special_tokens=True)[0]

print("finished: ", time.strftime("%H:%M:%S", time.localtime()))

print(generated_text)



