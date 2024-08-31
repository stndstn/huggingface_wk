# https://huggingface.co/impira/layoutlm-document-qa
# To run these examples, you must have PIL(pillow), pytesseract, and PyTorch installed in addition to transformers.
# pip install pillow pytesseract
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install transformers

from transformers import pipeline
import pytesseract
import PIL.Image as Image

# https://stackoverflow.com/questions/50655738/how-do-i-resolve-a-tesseractnotfounderror
# https://pypi.org/project/pytesseract/
pytesseract.pytesseract.tesseract_cmd = "C:\\Program Files\\Tesseract-OCR\\tesseract.exe"

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
outputs = nlp(
    [
        {"image": image, "question": "List all of the fields in this form."}
    ]
)
print(outputs)
#[{'score': 0.26583409309387207, 'answer': 'INDIVIDUAL APPLICATION FORM FOR DEPOSIT ACCOUNT', 'start': 2, 'end': 7}]

'''
outputs = nlp(
    [
        {"image": image, "question": "What is the title of this form?"},
        {"image": image, "question": "What is the text filled in the box 'Name (Last, Suffix, First, Middle)' in 'SECTION A PERSONAL INFORMATION'?"},
        {"image": image, "question": "What is the name of applicant in 'SECTION A PERSONAL INFORMATION'?"},
        {"image": image, "question": "What is the suffix of applicant in 'SECTION A PERSONAL INFORMATION' ?"}, 
    ]
)
print(outputs)


image2 = Image.open("..\\..\\images\\MYDL2.png")
outputs = nlp(
    [
        {"image": image2, "question": "What is this image?"},
        {"image": image2, "question": "What is the content of this card?"},
        {"image": image2, "question": "What is the text in the 1st line?"},
        {"image": image2, "question": "What is the text in the 2nd line?"},
        {"image": image2, "question": "What is the text in the last line?"},
        {"image": image2, "question": "What is the name"},
        {"image": image2, "question": "What is the address?"}
    ]
)
print(outputs)
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


