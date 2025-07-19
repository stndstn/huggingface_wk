# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# (python -m pip install wheel) 
# pip install flash-attn --no-build-isolation
# pip install transformers
# pip install num2words

import torch
#from transformers import AutoProcessor, AutoModelForVision2Seq
from transformers import AutoProcessor, AutoModelForImageTextToText
from PIL import Image

DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
#DEVICE = "cpu"
print(f"DEVICE: {DEVICE}")

image = Image.open("../images/MYDL1_s.jpg")

model_path = "HuggingFaceTB/SmolVLM2-2.2B-Instruct"
processor = AutoProcessor.from_pretrained(model_path)
model = AutoModelForImageTextToText.from_pretrained(
    model_path,
    torch_dtype=torch.bfloat16,
    _attn_implementation="flash_attention_2"
).to("cuda")

#print(f"model: {model}")

image = Image.open("../images/MYDL2.jpg")

# Create input messages
messages = [
    {
        "role": "user",
        "content": [
            {"type": "image", "image": image},
#            {"type": "text", "text": "Can you describe this image?"}
#            {"type": "text", "text": "What is the name of license holder?"}
#            {"type": "text", "text": "Please extract personal info of license holder from this imaghe of Malaysia Driving License."}
            {"type": "text", "text": "Please extract personal info of license holder of this Malaysian Driving License. Please answer in JSON format."}
        ]
    },
]

'''
generated_texts: ['User:\n\n\n\nPlease extract personal info of license holder from this imaghe of Malaysia Driving License.\nAssistant: The personal info of the license holder is as follows:\n\n- Name: TAKUMI TATEISHI\n- Nationality: JPN (Japan)\n- Date of Birth: 19/09/2016\n- Place of Birth: JPN (Japan)\n- Date of Issue: 18/04/2021\n- Place of Issue: JPN (Japan)\n- Expiry Date: 18/04/2022\n- License Number: TZ1145051JPN\n- License Type: Driving Licence\n- License Class: B2\n- Address: 42-12F CITY TOWER, JLN ALOR BKT BINTANG, 50200 KUALA LUMPUR, WILAYAH PERSEKUTUAN KUALA LUMPUR']

generated_texts: ['User:\n\n\n\nPlease extract personal info of license holder of this Malaysian Driving License. Please answer in JSON format.\nAssistant: {\n    "name": "TAKUMI TATEISHI",\n    "nationality": "JPN",\n    "date_of_birth": "19/09/2016",\n    "place_of_birth": "WANGANEGARA / KELAS / B2 D",\n    "address": "TEMPOH / Validity",\n    "identity_number": "TZ1145051JPN",\n    "license_type": "DRIVING LICENCE",\n    "issuing_authority": "LESEN MEMANDU",\n    "expiry_date": "18/04/2021",\n    "license_number": "JLN ALOR BKT BINTANG",\n    "place_of_issue": "JLN ALOR BKT BINTANG",\n    "place_of_issue_address": "50200 KUALA LUMPUR",\n    "issuing_authority_address": "WILAYAH PERSEKUTUAN KUALA LUMPUR"\n}']
'''



# Preprocess
inputs = processor.apply_chat_template(
    messages,
    add_generation_prompt=True,
    tokenize=True,
    return_dict=True,
    return_tensors="pt",
).to(model.device, dtype=torch.bfloat16)
#print(f"inputs: {inputs}")

generated_ids = model.generate(**inputs, do_sample=False, max_new_tokens=500)
generated_texts = processor.batch_decode(
    generated_ids,
    skip_special_tokens=True,
)
print(f"generated_texts: {generated_texts}")
#print(generated_texts[0])
