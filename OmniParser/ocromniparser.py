#from utils import get_som_labeled_img, check_ocr_box, get_caption_model_processor,  get_dino_model, get_yolo_model
import json
#from utils import get_som_labeled_img, check_ocr_box, get_caption_model_processor, get_yolo_model
from utils import get_som_labeled_img, check_ocr_box, get_yolo_model, predict_yolo, remove_overlap, get_parsed_content_icon, get_parsed_content_icon_phi3v, box_convert, annotate
#from utils import get_som_labeled_img, check_ocr_box, get_yolo_model
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
import torch
from ultralytics import YOLO
from PIL import Image
from typing import Dict, Tuple, List
import io
import base64
import numpy as np



config_cuda_blip2 = {
    'som_model_path': 'weights/icon_detect/best.pt',
    'model_name_or_path': 'weights/icon_caption_blip2',
    'device': 'cuda',
    'caption_model_path': 'Salesforce/blip2-opt-2.7b',
    'draw_bbox_config': {
        'text_scale': 0.8,
        'text_thickness': 2,
        'text_padding': 3,
        'thickness': 3,
    },
    'BOX_TRESHOLD': 0.05
}

config_cpu_blip2 = {
    'som_model_path': 'weights/icon_detect/best.pt',
    'model_name_or_path': 'weights/icon_caption_blip2',
    'device': 'cpu',
    'caption_model_path': 'Salesforce/blip2-opt-2.7b',
    'draw_bbox_config': {
        'text_scale': 0.8,
        'text_thickness': 2,
        'text_padding': 3,
        'thickness': 3,
    },
    'BOX_TRESHOLD': 0.05
}

config_cpu_florence2 = {
    'model_name': 'florence2',
    'model_name_or_path': 'weights/icon_caption_florence',
    'caption_model_path': 'finetuned_icon_detect.pt',
    'device': 'cpu',
    'caption_model_path': 'Salesforce/blip2-opt-2.7b',
    'draw_bbox_config': {
        'text_scale': 0.8,
        'text_thickness': 2,
        'text_padding': 3,
        'thickness': 3,
    },
    'BOX_TRESHOLD': 0.05
}

config_cuda_florence2 = {
    'model_name': 'florence2',
    'model_name_or_path': 'weights/icon_caption_florence',
    'caption_model_path': 'finetuned_icon_detect.pt',
    'device': 'cuda',
    'caption_model_path': 'Salesforce/blip2-opt-2.7b',
    'draw_bbox_config': {
        'text_scale': 0.8,
        'text_thickness': 2,
        'text_padding': 3,
        'thickness': 3,
    },
    'BOX_TRESHOLD': 0.05
}

config = config_cuda_blip2

class Omniparser(object):
    def __init__(self, config: Dict):
        self.config = config
        
        '''
        # https://github.com/microsoft/OmniParser/blob/master/demo.ipynb
        # two choices for caption model: fine-tuned blip2 or florence2

        # caption_model_processor = get_caption_model_processor(model_name="blip2", model_name_or_path="weights/icon_caption_blip2", device=device)
        caption_model_processor = get_caption_model_processor(model_name="florence2", model_name_or_path="weights/icon_caption_florence", device=device)
        '''
        self.som_model = get_yolo_model(model_path=config['som_model_path'])
        #self.caption_model_processor = get_caption_model_processor(config['model_name'], config['model_name_or_path'], device=config['device'])
        #self.caption_model_processor['model'].to(torch.float32)

    def parse(self, image_path: str):
        print('Parsing image_path:', image_path)
        ocr_bbox_rslt, is_goal_filtered = check_ocr_box(image_path, display_img = False, output_bb_format='xyxy', goal_filtering=None, easyocr_args={'paragraph': False, 'text_threshold':0.9})
        text, ocr_bbox = ocr_bbox_rslt

        draw_bbox_config = self.config['draw_bbox_config']
        BOX_TRESHOLD = self.config['BOX_TRESHOLD']
        dino_labled_img, label_coordinates, parsed_content_list = get_som_labeled_img(image_path, self.som_model, BOX_TRESHOLD = BOX_TRESHOLD, output_coord_in_ratio=False, ocr_bbox=ocr_bbox,draw_bbox_config=draw_bbox_config, caption_model_processor=None, ocr_text=text,use_local_semantics=False)
        
        image = Image.open(io.BytesIO(base64.b64decode(dino_labled_img)))
        # formating output
        return_list = [{'from': 'omniparser', 'shape': {'x':coord[0], 'y':coord[1], 'width':coord[2], 'height':coord[3]},
                        'text': parsed_content_list[i].split(': ')[1], 'type':'text'} for i, (k, coord) in enumerate(label_coordinates.items()) if i < len(parsed_content_list)]
        return_list.extend(
            [{'from': 'omniparser', 'shape': {'x':coord[0], 'y':coord[1], 'width':coord[2], 'height':coord[3]},
                        'text': 'None', 'type':'icon'} for i, (k, coord) in enumerate(label_coordinates.items()) if i >= len(parsed_content_list)]
              )

        return [image, return_list]

    def parseImage(self, image: Image):
        print('Parsing image:', image)
        if(image.width > image.height):
            if(image.width > 2000):
                scale = 2000 / image.width
                image = image.resize((int(image.width * scale), int(image.height * scale)))
                print('image resized:', image)
        else:
            if(image.height > 2000):
                scale = 2000 / image.height
                image = image.resize((int(image.width * scale), int(image.height * scale)))
                print('image resized:', image)


        # Convert the image to a NumPy array
        img_array = np.array(image)
        #print('img_array:', img_array)

        ocr_bbox_rslt, is_goal_filtered = check_ocr_box(img_array, display_img = False, output_bb_format='xyxy', goal_filtering=None, easyocr_args={'paragraph': False, 'text_threshold':0.9})
        text, ocr_bbox = ocr_bbox_rslt

        #draw_bbox_config = self.config['draw_bbox_config']
        #BOX_TRESHOLD = self.config['BOX_TRESHOLD']
        #boxes, parsed_content_list = self.extract_cord_content(image, self.som_model, ocr_bbox=ocr_bbox, ocr_text=text)
        boxes_xyxy, texts = self.extract_cord_content(image, self.som_model, ocr_bbox=ocr_bbox, ocr_text=text)
        print(f"parseImage boxes_xyxy: {boxes_xyxy}")
        print(f"parseImage texts: {texts}")
        return {'labels':texts, 'quad_boxes': boxes_xyxy}
        #text_boxes = []
        #if(len(boxes_xyxy) == len(texts)):
        #    text_boxes = [{'text': texts[i], 'boundary':boxes_xyxy[i].tolist()} for i in range(len(boxes_xyxy))]
        #print(f"parseImage parsed_content_list: {parsed_content_list}")
        #return [boxes, parsed_content_list]
        #return [boxes, ocr_text]
        #print(text_boxes)
        #return text_boxes

    '''
    def inference_yolo(self, model, image_source):
        """ Use huggingface model to replace the original model
        """
        # model = model['model']
        result = model(image_source)
        boxes = result[0].boxes.xyxy#.tolist() # in pixel space
        conf = result[0].boxes.conf
        phrases = [str(i) for i in range(len(boxes))]

        return boxes, conf, phrases
    '''

    #def extract_cord_content(self, img_src: Image, model=None, BOX_TRESHOLD = 0.01, output_coord_in_ratio=False, ocr_bbox=None, text_scale=0.4, text_padding=5, draw_bbox_config=None, caption_model_processor=None, ocr_text=[], use_local_semantics=True, iou_threshold=0.9,prompt=None,imgsz=640):
    def extract_cord_content(self, img_src: Image, model, ocr_bbox=None, ocr_text=[], iou_threshold=0.9):
        """ ocr_bbox: list of xyxy format bbox
        """
        #image_source = Image.open(img_path).convert("RGB")
        w, h = img_src.size
        # import pdb; pdb.set_trace()
        #xyxy, logits, phrases = self.inference_yolo(model, img_src)
        result = model(img_src)
        xyxy = result[0].boxes.xyxy#.tolist() # in pixel space
        #logits = result[0].boxes.conf
        #phrases = [str(i) for i in range(len(xyxy))]

        xyxy = xyxy / torch.Tensor([w, h, w, h]).to(xyxy.device)
        img_src = np.asarray(img_src)
        #phrases = [str(i) for i in range(len(phrases))]

        # annotate the image with labels
        h, w, _ = img_src.shape
        if ocr_bbox:
            ocr_bbox = torch.tensor(ocr_bbox) / torch.Tensor([w, h, w, h])
            ocr_bbox=ocr_bbox.tolist()
        else:
            print('no ocr bbox!!!')
            ocr_bbox = None
        filtered_boxes = remove_overlap(boxes=xyxy, iou_threshold=iou_threshold, ocr_bbox=ocr_bbox)
        
        #ocr_text = [f"Text Box ID {i}: {txt}" for i, txt in enumerate(ocr_text)]
        #parsed_content_merged = [f"{i}: {txt}" for i, txt in enumerate(ocr_text)]

        filtered_boxes = box_convert(boxes=filtered_boxes, in_fmt="xyxy", out_fmt="cxcywh")

        #phrases = [i for i in range(len(filtered_boxes))]
        
        # draw boxes
        '''
        if draw_bbox_config:
            annotated_frame, label_coordinates = annotate(image_source=img_src, boxes=filtered_boxes, logits=logits, phrases=phrases, **draw_bbox_config)
        else:
            annotated_frame, label_coordinates = annotate(image_source=img_src, boxes=filtered_boxes, logits=logits, phrases=phrases, text_scale=text_scale, text_padding=text_padding)
        
        return label_coordinates, parsed_content_merged
        '''        

        h, w, _ = img_src.shape
        boxes = filtered_boxes * torch.Tensor([w, h, w, h])
        xyxy = box_convert(boxes=boxes, in_fmt="cxcywh", out_fmt="xyxy").numpy()
        #xywh = box_convert(boxes=boxes, in_fmt="cxcywh", out_fmt="xywh").numpy()
        #label_coordinates = {f"{phrase}": v for phrase, v in zip(phrases, xywh)}
        #label_coordinates = {f"{phrase}": v for phrase, v in zip(phrases, xyxy)}

        #return boxes, parsed_content_merged
        return xyxy.tolist(), ocr_text


    def getDevice(self):
        return self.config['device']



'''
parser = Omniparser(config)
#image_path = 'examples/pc_1.png'
#image_path = 'id_images\\mydl\\IMG-4958_s.jpg'
image_path = 'form_images\\CSDEMOBANK_ApplicationForm_P1_s.jpeg'

#  time the parser
import time
s = time.time()
image, parsed_content_list = parser.parse(image_path)
device = config['device']
print(f'Time taken for Omniparser on {device}:', time.time() - s)
print(image)
print(parsed_content_list)

# save parsed_content_list
file_parsed_content_list = image_path + '_omniparser_parsed_content_list.json'
with open(file_parsed_content_list, 'w') as f:
    f.write(f"{parsed_content_list}")
    f.close()

# save the image with frame
image.save(image_path + '_omniparser.png')
image.show()
'''    
