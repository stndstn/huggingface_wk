# pip install pillow
# pip install requests
# pip install transformers
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124

import time
from PIL import Image
# import requests

from transformers import CLIPProcessor, CLIPModel

model = CLIPModel.from_pretrained("openai/clip-vit-base-patch32")
processor = CLIPProcessor.from_pretrained("openai/clip-vit-base-patch32")

def readCheckboxImage(image):
    t_start = time.localtime()    
    print('start: ', t_start.tm_hour, ':', t_start.tm_min, ':', t_start.tm_sec)

    inputs = processor(text=["checked", "empty"], images=image, return_tensors="pt", padding=True)
    outputs = model(**inputs)
    logits_per_image = outputs.logits_per_image  # this is the image-text similarity score
    print(logits_per_image)
    probs = logits_per_image.softmax(dim=1)  # we can take the softmax to get the label pr
    print(probs)
    print(probs[0])
    print(probs[0][0])
    print(probs[0][1])
    if(probs[0][0] > probs[0][1]):
        print("checked")
        return True
    else:    
        print("unchecked")
        return False

    t_end = time.localtime()
    print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
    print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)


#url = "http://images.cocodataset.org/val2017/000000039769.jpg"
#image = Image.open(requests.get(url, stream=True).raw)
#inputs = processor(text=["a photo of a cat", "a photo of a dog"], images=image, return_tensors="pt", padding=True)


#image1 = Image.open("..\\images\\Checked_1.png")
ret = readCheckboxImage(Image.open("..\\images\\Checked_1.png"))
print(ret)
#image2 = Image.open("..\\images\\Unchecked_1.png")
ret = readCheckboxImage(Image.open("..\\images\\Unchecked_1.png"))
print(ret)
#image3 = Image.open("..\\images\\Checked_2.jpeg")
ret = readCheckboxImage(Image.open("..\\images\\Checked_2.jpeg"))
print(ret)
#image4 = Image.open("..\\images\\Unchecked_2.jpeg")
ret = readCheckboxImage(Image.open("..\\images\\Unchecked_2.jpeg"))
print(ret)




