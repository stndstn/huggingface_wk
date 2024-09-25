# https://huggingface.co/impira/layoutlm-document-qa
# To run these examples, you must have PIL(pillow), pytesseract, and PyTorch installed in addition to transformers.
# pip install pillow pytesseract
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install transformers

from transformers import pipeline
import pytesseract
import PIL.Image as Image
import time

print("start: ", time.strftime("%H:%M:%S", time.localtime()))

# https://stackoverflow.com/questions/50655738/how-do-i-resolve-a-tesseractnotfounderror
# https://pypi.org/project/pytesseract/
pytesseract.pytesseract.tesseract_cmd = "C:\\Program Files\\Tesseract-OCR\\tesseract.exe"

print("calling pipeline...", time.strftime("%H:%M:%S", time.localtime()))
nlp = pipeline(
    "document-question-answering",
    model="impira/layoutlm-document-qa",
    device="cuda",
)

#outputs = nlp(
#    "https://templates.invoicehome.com/invoice-template-us-neat-750px.png",
#    "What is the invoice number?"
#)
# {'score': 0.9943977, 'answer': 'us-001', 'start': 15, 'end': 15}


image = Image.open("..\\..\\images\\CSDEMOBANK.jpg")
print("calling nlp...", time.strftime("%H:%M:%S", time.localtime()))
outputs = nlp(
    [
        {"image": image, "question": "What is the title of this form?"},
        #{"image": image, "question": "What is the text filled in the box 'Name (Last, Suffix, First, Middle)' in 'SECTION A PERSONAL INFORMATION'?"},
        {"image": image, "question": "What is the 'Name' in 'SECTION A PERSONAL INFORMATION'?"},
        {"image": image, "question": "Whuch checkbox is selected in the box 'Name' in 'SECTION A PERSONAL INFORMATION'?"},
        {"image": image, "question": "What is the status of checkbox 'Mr' in 'SECTION A PERSONAL INFORMATION'?"}, 
        {"image": image, "question": "What is the status of checkbox 'Mrs' in 'SECTION A PERSONAL INFORMATION'?"}, 
        {"image": image, "question": "What is the text filled in the box 'Date of Birth (mm/dd/yyyy)' in 'SECTION A PERSONAL INFORMATION'?"}, 
        {"image": image, "question": "What is the text filled in the box 'Place of Birth' in 'SECTION A PERSONAL INFORMATION'?"}, 
    ]
)
print(outputs)
'''
[[{'score': 0.26583409309387207, 'answer': 'INDIVIDUAL APPLICATION FORM FOR DEPOSIT ACCOUNT', 'start': 2, 'end': 7}], 
[{'score': 0.9994979500770569, 'answer': 'PERE? FELIX', 'start': 28, 'end': 29}], 
[{'score': 0.9968395829200745, 'answer': 'PERE? FELIX', 'start': 28, 'end': 29}], 
[{'score': 0.5968350768089294, 'answer': 'Marital Status', 'start': 41, 'end': 42}], 
[{'score': 0.4746653139591217, 'answer': 'Marital Status', 'start': 41, 'end': 42}], 
[{'score': 0.39794379472732544, 'answer': 'BULACAN', 'start': 40, 'end': 40}], 
[{'score': 0.9995900988578796, 'answer': 'BULACAN', 'start': 40, 'end': 40}]]
'''
image2 = Image.open("..\\..\\images\\MYDL2.png")
print("calling nlp...", time.strftime("%H:%M:%S", time.localtime()))
outputs = nlp(
    [
        {"image": image2, "question": "List all fields in this image in json format."},
        {"image": image2, "question": "What is 'Warganegara / Nationality'?"},
        {"image": image2, "question": "What is 'No. Penganaran / Identity Number'?"},
        {"image": image2, "question": "What is 'Tempoh / Validity'?"},
        {"image": image2, "question": "What is the line under 'Tempoh / Validity'?"},
        {"image": image2, "question": "What is 'Alamat / Address'?"},
        {"image": image2, "question": "What are all lines under 'Alamat / Address'?"},
        #{"image": image2, "question": "What is this image?"},
        #{"image": image2, "question": "What is the content of this card?"},
        #{"image": image2, "question": "What is the text in the 1st line?"},
        #{"image": image2, "question": "What is the text in the 2nd line?"},
        #{"image": image2, "question": "What is the text in the last line?"},
        #{"image": image2, "question": "What is the name"},
        #{"image": image2, "question": "What is the address?"}
    ]
)
print(outputs)
'''
[[{'score': 0.6096019744873047, 'answer': 'B2D', 'start': 19, 'end': 19}], 
[{'score': 0.8405320048332214, 'answer': 'JPN', 'start': 14, 'end': 14}], 
[{'score': 0.9275990128517151, 'answer': 'TZ1145051JPN', 'start': 15, 'end': 15}], 
[{'score': 0.9862936735153198, 'answer': '19/09/2016', 'start': 26, 'end': 26}], 
[{'score': 0.9948939681053162, 'answer': '19/09/2016', 'start': 26, 'end': 26}], 
[{'score': 0.7095350027084351, 'answer': '42-12F CITY TOWER', 'start': 31, 'end': 33}], 
[{'score': 0.9941417574882507, 'answer': '42-12F', 'start': 31, 'end': 31}]]
'''

#outputs = nlp(
#    "https://miro.medium.com/max/787/1*iECQRIiOGTmEFLdWkVIH2g.jpeg",
#    "What is the purchase amount?"
#)
# {'score': 0.9912159, 'answer': '$1,000,000,000', 'start': 97, 'end': 97}
#print(outputs)

#outputs = nlp(
#    "https://www.accountingcoach.com/wp-content/uploads/2013/10/income-statement-example@2x.png",
#    "What are the 2020 net sales?"
#)
# {'score': 0.59147286, 'answer': '$ 3,750', 'start': 19, 'end': 20}
#print(outputs)

print("finished: ", time.strftime("%H:%M:%S", time.localtime()))

