# pip install torch==2.7.1 torchvision==0.22.1 torchaudio==2.7.1 --index-url https://download.pytorch.org/whl/cu128
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu128

# pip install git+https://github.com/huggingface/transformers
# pip3 install torch torchvision --index-url https://download.pytorch.org/whl/cu126
# pip install accelerate
# python -m pip install wheel
# pip install flash-attn --no-build-isolation
# pip install flash-attn===1.0.4 --no-build-isolation
# pip install hf_xet

import time
from transformers import Qwen3VLForConditionalGeneration, AutoProcessor
import torch
from PIL import Image

#image = Image.open("./images/TAU integration diagram.jpg")
image = Image.open("./images/TAU integration diagram 2.jpg")

# default: Load the model on the available device(s)
#model = Qwen3VLForConditionalGeneration.from_pretrained(
#    "Qwen/Qwen3-VL-4B-Thinking", dtype="auto", device_map="auto"
#)

# We recommend enabling flash_attention_2 for better acceleration and memory saving, especially in multi-image and video scenarios.
model = Qwen3VLForConditionalGeneration.from_pretrained(
#     "Qwen/Qwen3-VL-4B-Thinking",
     "Qwen/Qwen3-VL-2B-Thinking",
     dtype=torch.bfloat16,
     #attn_implementation="flash_attention_2",
     device_map="auto",
 )

#processor = AutoProcessor.from_pretrained("Qwen/Qwen3-VL-4B-Thinking")
processor = AutoProcessor.from_pretrained("Qwen/Qwen3-VL-2B-Thinking")

t_start = time.localtime()
print('start: ', t_start.tm_hour, ':', t_start.tm_min, ':', t_start.tm_sec)

messages = [
    {
        "role": "user",
        "content": [
            {
                "type": "image",
#                "image": "https://qianwen-res.oss-cn-beijing.aliyuncs.com/Qwen-VL/assets/demo.jpeg",
                "image": image,
            },
            {"type": "text", "text": "Describe this image."},
        ],
    }
]

# Preparation for inference
inputs = processor.apply_chat_template(
    messages,
    tokenize=True,
    add_generation_prompt=True,
    return_dict=True,
    return_tensors="pt"
)
inputs = inputs.to(model.device)

# Inference: Generation of the output
generated_ids = model.generate(**inputs, max_new_tokens=128)
generated_ids_trimmed = [
    out_ids[len(in_ids) :] for in_ids, out_ids in zip(inputs.input_ids, generated_ids)
]
output_text = processor.batch_decode(
    generated_ids_trimmed, skip_special_tokens=True, clean_up_tokenization_spaces=False
)
print(output_text)

t_end = time.localtime()
print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)


'''

*TAU integration diagram.jpg
**Qwen/Qwen3-VL-4B-Thinking
'So, let\'s look at the image. It\'s a diagram about TAU (Teller Automation Unit) Integration. 
The title is at the top. Then there\'s a flowchart with several blue rectangles connected by arrows. 
Let\'s list each component.\n\nFirst, the topmost is MV-FE. Then below it is CSDI Web API TAU client service (with .Net Framework). 
Next is Wrapper Library, C++/CLR (.Net Framework). Then Glory TAU DLLs. 
From Glory TAU DLLs, an arrow goes out to the right, labeled "Network", leading to a box that says TAU device'
**Qwen/Qwen3-VL-2B-Thinking
TAU integration diagram.jpg
'Got it, let\'s describe this image. First, the title is "TAU (Teller Automation Unit) Integration". 
The image is a flowchart showing the integration process. Let\'s break down each component.\n\n
Starting from the top: there\'s a blue rectangle labeled "MV-FE". That\'s probably the Mobile View Frontend or something related to the front-end. 
Then an arrow points down to the next box. The second box is "CSDI Web API TAU client service (.Net Framework)". 
So this is the client service that interacts with the TAU system. Then another arrow down to "Wrapper Library C++/CLR']

*TAU integration diagram 2.jpg
**Qwen/Qwen3-VL-4B-Thinking
'So, let\'s try to describe this image step by step. First, I need to look at the components and their relationships. 
The image is a diagram showing a system architecture, probably for a software or network setup. \n\n
There\'s a large rounded rectangle labeled "< Branch >", which seems to be the main container for the system. 
Inside this container, there are two identical blue rectangular blocks, each with three components: FE (HTML5), CSDI (WebAPI), and DLL SDK. 
These are stacked vertically, so FE is on top, then CSDI, then DLL SDK. Each of these blocks has an arrow going downward'
**Qwen/Qwen3-VL-2B-Thinking
'So, let\'s break down the image step by step. First, the main elements: there\'s a large rounded rectangle labeled "< Branch >" at the top. 
Inside that, there are two blue boxes with a similar structure—each has FE (HTML5), CSDI (WebAPI), and DLL SDK stacked. \n\n
On the left, there\'s a blue rectangle labeled "TAU Device" connected by an orange arrow to the left side of the top blue box. 
The top blue box has an orange line labeled "Network" going from the left to the top box, then the top box has connections to the "BE"'
'''
