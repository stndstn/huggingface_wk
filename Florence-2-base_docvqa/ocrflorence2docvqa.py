# pip install einops timm
# pip install "numpy<2.0"
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu126
# pip install psutil
# (python -m pip install wheel) 
# pip install flash-attn --no-build-isolation
# pip install transformers
'''
Florence2LanguageForConditionalGeneration has generative capabilities, as prepare_inputs_for_generation is explicitly overwritten. 
However, it doesn't directly inherit from GenerationMixin. 
From 👉v4.50👈 onwards, PreTrainedModel will NOT inherit from GenerationMixin, and this model will lose the ability to call generate and other related functions.
If you're using trust_remote_code=True, you can get rid of this warning by loading the model with an auto class. See https://huggingface.co/docs/transformers/en/model_doc/auto#auto-classes
If you are the owner of the model architecture code, please modify your model class such that it inherits from GenerationMixin (after PreTrainedModel, otherwise you'll get an exception).
'''
# (install rust compiler, then 'pip install "transformers==4.44.2"'
# pip install flask

# RMKS: if install by requirements.txt does not to work. delete all cache and re-install with pip manually 
## pip freeze > requirements.txt
## pip install -r requirements.txt


import requests
import torch

from PIL import Image
from transformers import AutoProcessor, AutoModelForCausalLM

#model_name = "microsoft/Florence-2-large"
'''
docVQA parsed_answer: {'<DocVQA>': 'LESEN MEMANDU\nMALAYSIA\nDRIVING LICENCE\nTAKUMI TATEISHI\nJPN\nTZ11145051JPN\nB2 0\nB3 0\n12-12-2016-18-04-2021\n42-12:12 CITY TOWER\nJLN ALOR BKT BINTANG\n5020 KUALA LUMPUR\nWILAKAR PERSERKUTUAN KULA LUMPUR\n'}
{'<DocVQA>': 'LESEN MEMANDU\nMALAYSIA\nDRIVING LICENCE\nTAKUMI TATEISHI\nJPN\nTZ11145051JPN\nB2 0\nB3 0\n12-12-2016-18-04-2021\n42-12:12 CITY TOWER\nJLN ALOR BKT BINTANG\n5020 KUALA LUMPUR\nWILAKAR PERSERKUTUAN KULA LUMPUR\n'}
LESEN MEMANDU
MALAYSIA
DRIVING LICENCE
TAKUMI TATEISHI
JPN
TZ11145051JPN
B2 0
B3 0
12-12-2016-18-04-2021
42-12:12 CITY TOWER
JLN ALOR BKT BINTANG
5020 KUALA LUMPUR
WILAKAR PERSERKUTUAN KULA LUMPUR
'''
#model_name = "microsoft/Florence-2-large-ft"
'''
docVQA parsed_answer: {'<DocVQA>': 'takumi tateishi'}
{'<DocVQA>': 'takumi tateishi'}
takumi tateishi
docVQA parsed_answer: {'<DocVQA>': 'unanswerable'}
{'<DocVQA>': 'unanswerable'}
unanswerable
docVQA parsed_answer: {'<DocVQA>': 'unanswerable'}
{'<DocVQA>': 'unanswerable'}
unanswerable
'''
#model_name = "HuggingFaceM4/Florence-2-DocVQA"
'''
{'<DocVQA>': 'The name of the driver is Takumi Tateishi.'}
The name of the driver is Takumi Tateishi.
docVQA parsed_answer: {'<DocVQA>': 'The address is 42-127 City Tower, JLN ALOR BKT BINTANG, 50020 KUALA LUMPUR.'}
{'<DocVQA>': 'The address is 42-127 City Tower, JLN ALOR BKT BINTANG, 50020 KUALA LUMPUR.'}
The address is 42-127 City Tower, JLN ALOR BKT BINTANG, 50020 KUALA LUMPUR.
docVQA parsed_answer: {'<DocVQA>': 'The document number is TZ1145051JPN.'}
{'<DocVQA>': 'The document number is TZ1145051JPN.'}
The document number is TZ1145051JPN.
'''
#model_name = "microsoft/Florence-2-base-ft"
'''
docVQA parsed_answer: {'<DocVQA>': 'unanswerable'}
{'<DocVQA>': 'unanswerable'}
unanswerable
docVQA parsed_answer: {'<DocVQA>': 'unanswerable'}
{'<DocVQA>': 'unanswerable'}
unanswerable
docVQA parsed_answer: {'<DocVQA>': 'unanswerable'}
{'<DocVQA>': 'unanswerable'}
unanswerable
'''
#model_name = "..\\florence2-finetuning-main\\model_checkpoints\\epoch_1" # trained "microsoft/Florence-2-base-ft"
'''
{'<DocVQA>': '.'}
.
docVQA parsed_answer: {'<DocVQA>': '.'}
{'<DocVQA>': '.'}
.
docVQA parsed_answer: {'<DocVQA>': '.'}
{'<DocVQA>': '.'}
.
'''
print(model_name)

# HuggingFaceM4/Florence-2-DocVQA needs 'pip install timm'
#model_name = "HuggingFaceM4/Florence-2-DocVQA"
#print("Florence-2-DocVQA...")

device = "cuda:0" if torch.cuda.is_available() else "cpu"
print(f"deivice: {device}")
torch_dtype = torch.float16 if torch.cuda.is_available() else torch.float32
print(f"torch_dtype: {torch_dtype}")

# Assert config.vision_config.model_type == 'davit', 'only DaViT is supported for now'
# https://huggingface.co/microsoft/Florence-2-large/discussions/44
# in config.json file search for 'model_type' inside 'vision_config', and replace its value with 'davit'

model = AutoModelForCausalLM.from_pretrained(model_name, torch_dtype=torch_dtype, trust_remote_code=True).to(device)
print(f"model: {model}")
processor = AutoProcessor.from_pretrained(model_name, trust_remote_code=True)
print(f"processor: {processor}")

def getDevice():
    return device

# https://huggingface.co/microsoft/Florence-2-large/blob/main/sample_inference.ipynb
# Run pre-defined tasks without additional inputs
## Caption: <CAPTION>, <DETAILED_CAPTION>, <MORE_DETAILED_CAPTION>, 
## Object detection: <OD>, 
## Dense region caption: <DENSE_REGION_CAPTION>, 
## Region proposal: <REGION_PROPOSAL>, 
## ocr related tasks: <OCR>, <OCR_WITH_REGION>, 
# Run pre-defined tasks that requires additional inputs
## Phrase Grounding: <CAPTION_TO_PHRASE_GROUNDING>, 
## Referring expression segmentation: <REFERRING_EXPRESSION_SEGMENTATION>
## Region to segmentation: <REGION_TO_SEGMENTATION>, 
## Open vocabulary detection: <OPEN_VOCABULARY_DETECTION>, 
## Region to texts: <REGION_TO_CATEGORY>, <REGION_TO_DESCRIPTION>


def ocr(image, task_prompt="<OCR>"):
    #print(f"ocr image: {image}, task_prompt: {task_prompt}")
    # url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/car.jpg?download=true"
    # image = Image.open(requests.get(url, stream=True).raw)
    # image = Image.open("..\\..\\images\\MYDL1_s.jpg")
    # image = Image.open("..\\..\\images\\handwritten1.jpg")

    inputs = processor(text=task_prompt, images=image, return_tensors="pt").to(device, torch_dtype)
    #print(f"ocr inputs: {inputs}")

    generated_ids = model.generate(
        input_ids=inputs["input_ids"],
        pixel_values=inputs["pixel_values"],
        max_new_tokens=16384,
        do_sample=False,
        num_beams=3,
    )
    #print(f"ocr generated_ids: {generated_ids}")
    generated_text = processor.batch_decode(generated_ids, skip_special_tokens=False)[0]
    #print(f"ocr generated_text: {generated_text}")

    parsed_answer = processor.post_process_generation(generated_text, task=task_prompt, image_size=(image.width, image.height))
    print(f"ocr parsed_answer: {parsed_answer}")
    return parsed_answer

def docVqa(image, text_input=None, task_prompt="<DocVQA>"):
    if text_input is None:
        prompt = task_prompt
    else:
        prompt = task_prompt + text_input

    inputs = processor(text=prompt, images=image, return_tensors="pt").to(device, torch_dtype)
    #print(f"ocr inputs: {inputs}")

    generated_ids = model.generate(
        input_ids=inputs["input_ids"],
        pixel_values=inputs["pixel_values"],
        max_new_tokens=16384,
        do_sample=False,
        num_beams=3,
    )
    #print(f"ocr generated_ids: {generated_ids}")
    generated_text = processor.batch_decode(generated_ids, skip_special_tokens=False)[0]
    #print(f"ocr generated_text: {generated_text}")

    parsed_answer = processor.post_process_generation(generated_text, task=task_prompt, image_size=(image.width, image.height))
    print(f"docVQA parsed_answer: {parsed_answer}")
    return parsed_answer


'''
image = Image.open("..\\..\\images\\handwritten1.jpg")
ocr_answer = ocr(image)
print(ocr_answer['<OCR>'])
# {'<OCR>': "Frank-Sweetie I amokay. I'm wl myoffice overbyThe Lyndon B. Johnsonmemorial"}
'''
image = Image.open("./images/MYDL1_s.jpg")
docvqa_answer = docVqa(image, text_input="What is the name?")
print(docvqa_answer)
print(docvqa_answer['<DocVQA>'])
# {'<DocVQA>': 'takumi tateishi'}

docvqa_answer = docVqa(image, text_input="What is the address?")
print(docvqa_answer)
print(docvqa_answer['<DocVQA>'])
# {'<DocVQA>': 'unanswerable'}

docvqa_answer = docVqa(image, text_input="What is the document number?")
print(docvqa_answer)
print(docvqa_answer['<DocVQA>'])
# {'<DocVQA>': 'unanswerable'}
