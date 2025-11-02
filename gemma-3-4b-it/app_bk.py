# https://huggingface.co/google/gemma-3-4b-it
# pip install git+https://github.com/huggingface/transformers@v4.49.0-Gemma-3
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install accelerate
# pip install pillow

# huggingface-cli login --token hf_...


from transformers import AutoProcessor, AutoModelForImageTextToText
#from transformers import AutoProcessor, Gemma3ForConditionalGeneration
#from transformers import pipeline
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
#os.environ["PATH"] = f"{huggingface_bin_path}:{os.environ['PATH']}"
#subprocess.run(["huggingface-cli", "login", "--token", my_secret.hf_token], shell=True)
#subprocess.run(["huggingface-cli", "login", "--token", my_secret.hf_token])

model_id = "google/gemma-3-4b-it"
device = "cuda"


'''
# pipeline
pipe = pipeline(
    "image-text-to-text",
    model=model_id,
    device="cuda",
    torch_dtype=torch.bfloat16
)
'''

'''
# Gemma3ForConditionalGeneration
model = Gemma3ForConditionalGeneration.from_pretrained(
    model_id, device_map="auto"
).eval()

processor = AutoProcessor.from_pretrained(model_id)
'''

# use processor
# Load model directly with AutoModelForImageTextToText
processor = AutoProcessor.from_pretrained(model_id)
model = AutoModelForImageTextToText.from_pretrained(model_id, token=my_secret.hf_token).to(device).eval()

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
print('start: ', t_start.tm_hour, ':', t_start.tm_min, ':', t_start.tm_sec)

# use processor
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
output = pipe(text=messages, max_new_tokens=200)
print(output[0][0]["generated_text"][-1]["content"])
'''


t_end = time.localtime()

print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)


'''
(.venv) PS C:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it>  c:; cd 'c:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it'; & 'c:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it\.venv\Scripts\python.exe' 'c:\Users\li4sh\.vscode\extensions\ms-python.debugpy-2025.4.1-win32-x64\bundled\libs\debugpy\launcher' '54448' '--' 'C:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it\app.py' 
Using a slow image processor as `use_fast` is unset and a slow processor was saved with this model. `use_fast=True` will be the default behavior in v4.48, even if the model was saved with a slow processor. This will result in minor differences in outputs. You'll still be able to use a slow processor with `use_fast=False`.
Loading checkpoint shards: 100%|██████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████| 2/2 [00:05<00:00,  2.74s/it]
Okay, I've carefully reviewed the bank account application form. Here's the extracted information about the applicant, Felix Perez:

**Personal Information:**

*   **Full Name:** Felix Perez
*   **Date of Birth:** 08/26/1979
*   **Marital Status:** Single
*   **Mother’s Maiden Name:** Ermita
*   **Spouse’s Name:**  Spouse Name Not Listed
*   **Nationality:** Philippine
*   **Resident Status:** Resident
*   **Gender:** Male (indicated by the “M” box)

**Contact Information:**

*   **Permanent Address:** 823 PASEO DE ROKSAS ST., MAKATI CITY, 1226 PHILIPPINES
*   **Mailing Address:** 823 PASEO DE ROKSAS ST., MAKATI CITY, 1226 PHILIPPINES
*   **Mobile Number:** 63 917 726 8115
*   **Email:** felix.perez@gmail.com
*   **Employer/Business Address:** SAS PHILIPPINES
*   **Employer/Business Contact:** 63 2 818 3347
*   **Employer/Business Email:** s/es@sas.com.ph

**Employment Information:**

*   **Employment Status:** Permanent
*   **Occupation:** Accountant
*   **Designation:** Manager

**Other Information:**

*   **Number of Children:** 0
*   **Primary ID:** CRM 038214708256
*   **Umid:** Not Listed
*   **Expiry Date:** Not Listed

---

Do you need any specific information extracted from the form, or would you like me to look for something in particular?
end:  13 : 1 : 40
elapsed:  1 : -53 : 31
(.venv) PS C:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it> ^C
(.venv) PS C:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it>
(.venv) PS C:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it>  c:; cd 'c:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it'; & 'c:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it\.venv\Scripts\python.exe' 'c:\Users\li4sh\.vscode\extensions\ms-python.debugpy-2025.4.1-win32-x64\bundled\libs\debugpy\launcher' '49801' '--' 'C:\Users\li4sh\Documents\huggingface\wk\gemma-3-4b-it\app.py'
Using a slow image processor as `use_fast` is unset and a slow processor was saved with this model. `use_fast=True` will be the default behavior in v4.48, even if the model was saved with a slow processor. This will result in minor differences in outputs. You'll still be able to use a slow processor with `use_fast=False`.
Loading checkpoint shards: 100%|██████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████| 2/2 [00:05<00:00,  2.62s/it]
start:  14 : 13 : 13
Okay, I've carefully reviewed the bank account application form. Here's the extracted information about the applicant, Felix Perez:

**Personal Information:**

*   **Full Name:** Felix Perez
*   **Date of Birth:** 08/26/1979
*   **Marital Status:** Single
*   **Mother’s Maiden Name:** Ermita
*   **Spouse’s Name:**  Spouse Name Not Listed
*   **Nationality:** Philippine
*   **Resident Status:** Resident
*   **Gender:** Male (indicated by the “M” box)

**Contact Information:**

*   **Permanent Address:** 823 PASEO DE ROKSAS ST., MAKATI CITY, 1226 PHILIPPINES
*   **Mailing Address:** 823 PASEO DE ROKSAS ST., MAKATI CITY, 1226 PHILIPPINES
*   **Mobile Number:** 63 917 726 8115
*   **Email:** felix.perez@gmail.com
*   **Employer/Business Address:** SAS PHILIPPINES
*   **Employer/Business Contact:** 63 2 818 3347
*   **Employer/Business Email:** s/es@sas.com.ph

**Employment Information:**

*   **Employment Status:** Permanent
*   **Occupation:** Accountant
*   **Designation:** Manager

**Other Information:**

*   **Number of Children:** 0
*   **Primary ID:** CRM 038214708256
*   **Umid:** Not Listed
*   **Expiry Date:** Not Listed

---

Do you need any specific information extracted from the form, or would you like me to look for something in particular?
end:  14 : 20 : 44
elapsed:  0 : 7 : 31
'''
