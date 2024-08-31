#pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
#pip install transformers einops Pillow
#pip install "numpy<2.0"

from transformers import AutoModelForCausalLM, AutoTokenizer
from PIL import Image

model_id = "vikhyatk/moondream2"
revision = "2024-08-26"
model = AutoModelForCausalLM.from_pretrained(
    model_id, trust_remote_code=True, revision=revision
)
tokenizer = AutoTokenizer.from_pretrained(model_id, revision=revision)

image = Image.open('..\\images\\MYDL2.png')
#image = Image.open('..\\images\\CSDEMOBANK.jpg')
enc_image = model.encode_image(image)

#print(model.answer_question(enc_image, "Describe this image.", tokenizer))
print(model.answer_question(enc_image, "List all fields in this image.", tokenizer))
#print(model.answer_question(enc_image, "What is the text filled under 'Name (First, Sufix, Last, Middle)'?", tokenizer))
#print(model.answer_question(enc_image, "What is the place of birth?", tokenizer))
#print(model.answer_question(enc_image, "What is the permenent address?", tokenizer))
#print(model.answer_question(enc_image, "How many fields exist in this form?", tokenizer))
