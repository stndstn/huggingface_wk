# https://flask.palletsprojects.com/en/3.0.x/quickstart/
# https://code.visualstudio.com/docs/python/tutorial-flask

import base64
import datetime
import os
import imghdr
import io
from PIL import Image
from flask import Flask, request, jsonify
from werkzeug.utils import secure_filename
from werkzeug.exceptions import HTTPException
# pip install flask

import ocrflorence2base

#UPLOAD_FOLDER = '/path/to/the/uploads'
UPLOAD_FOLDER = 'c:\\temp\\flask_test\\uploads'
ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg'}

app = Flask(__name__)
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER


class UnknownError(Exception):
    status_code = 400

    def __init__(self, message, status_code=None, payload=None):
        super().__init__()
        self.message = message
        if status_code is not None:
            self.status_code = status_code
        self.payload = payload

    def to_dict(self):
        rv = dict(self.payload or ())
        rv['message'] = self.message
        return rv

@app.errorhandler(UnknownError)
def invalid_api_usage(e):
    return jsonify(e.to_dict()), e.status_code


@app.route("/")
def hello_world():
    return "<p>Hello, World!</p>"

@app.route('/testjson', methods=['POST'])
def login():
    content = request.json
    print(content)
    return content

def allowed_file(filename):
    return '.' in filename and \
           filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


@app.route('/device')
def device():
    return ocrflorence2base.getDevice()


@app.route('/ocrfile', methods=['POST'])
@app.route('/ocrFile', methods=['POST'])
def ocrFile():
    if request.method == 'POST':        
        # check if the post request has the file part
        if 'file' not in request.files:
            raise UnknownError("'file' not in request.files")
        
        file = request.files['file']
        # If the user does not select a file, the browser submits an
        # empty file without a filename.        
        if file == None or file.filename == '':
            raise UnknownError('No selected file')

        task_prompt = "<OCR>"
        if('task_prompt' in request.form):
            task_prompt = request.form['task_prompt']

        # if file is not allowed filetype, return error
        if not allowed_file(file.filename):
            raise UnknownError("File type not acceptable")
        
        #filename = secure_filename(file.filename)
        #filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        #file.save(filepath)
        #image = Image.open(filepath)

        # Read the file content into a bytearray
        img_data = bytearray(file.read())
        image = Image.open(io.BytesIO(img_data))
        w = image.width
        h = image.height
        scale = 800 / max(w, h)
        if(scale < 1):
            image = image.resize((int(w * scale), int(h * scale)))

        ret = ocrflorence2base.ocr(image, task_prompt)
        if '<OCR_WITH_REGION>' in ret:
            ret["image_width"] = image.width;
            ret["image_height"] = image.height;

        return ret

    return 


@app.route('/ocrWithRegionFile', methods=['POST'])
@app.route('/ocrwithregionfile', methods=['POST'])
def ocrWithRegionFile():
    if request.method == 'POST':        
        # check if the post request has the file part
        if 'file' not in request.files:
            raise UnknownError("'file' not in request.files")
        
        file = request.files['file']
        # If the user does not select a file, the browser submits an
        # empty file without a filename.        
        if file == None or file.filename == '':
            raise UnknownError('No selected file')

        task_prompt = "<OCR_WITH_REGION>"

        # if file is not allowed filetype, return error
        if not allowed_file(file.filename):
            raise UnknownError("File type not acceptable")
        
        #filename = secure_filename(file.filename)
        #filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        #file.save(filepath)
        #image = Image.open(filepath)

        # Read the file content into a bytearray
        img_data = bytearray(file.read())
        image = Image.open(io.BytesIO(img_data))
        w = image.width
        h = image.height
        scale = 800 / max(w, h)
        if(scale < 1):
            image = image.resize((int(w * scale), int(h * scale)))

        ret = ocrflorence2base.ocr(image, task_prompt)
        if '<OCR_WITH_REGION>' in ret:
            ret["image_width"] = image.width;
            ret["image_height"] = image.height;
        
        return ret
        
    return 


@app.route('/ocrb64', methods=['POST'])
@app.route('/ocrB64', methods=['POST'])
def ocrB64():
    if request.method == 'POST':        
        # check if the post request is json
        # if request i snot json return error
        if not request.is_json:
            raise UnknownError("request is not json")
        #print(request.json)
        b64 = request.json['b64']
        if b64 is None:
            raise UnknownError("request json does not contain 'b64'")

        task_prompt = "<OCR>"
        if('task_prompt' in request.json):
            task_prompt = request.json['task_prompt']

        # convert base64 string to bytearray
        img_data = base64.b64decode(b64)
        whatfile = imghdr.what(None, img_data)
        #print(whatfile)

        # save to disk
        # set filename as yyyymmddhhmmssfff formatted date now
        #now = datetime.datetime.now()
        #filename = now.strftime("%Y%m%d%H%M%S%f") + '.' + whatfile
        #filename = secure_filename(filename)
        #filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        #with open(filepath, 'wb') as f:
        #    f.write(img_data)

        image = Image.open(io.BytesIO(img_data))
        w = image.width
        h = image.height
        scale = 800 / max(w, h)
        if(scale < 1):
            image = image.resize((int(w * scale), int(h * scale)))

        ret = ocrflorence2base.ocr(image, task_prompt)
        # check if ret has <OCR_WITH_REGION>
        if '<OCR_WITH_REGION>' in ret:
            ret["image_width"] = image.width;
            ret["image_height"] = image.height;
        
        return ret
        
    return 


@app.route('/ocrwithregionb64', methods=['POST'])
@app.route('/ocrWithRegionB64', methods=['POST'])
def ocrWithRegionB64():
    if request.method == 'POST':        
        # check if the post request is json
        # if request i snot json return error
        if not request.is_json:
            raise UnknownError("request is not json")
        #print(request.json)
        b64 = request.json['b64']
        if b64 is None:
            raise UnknownError("request json does not contain 'b64'")

        task_prompt = "<OCR_WITH_REGION>"

        # convert base64 string to bytearray
        img_data = base64.b64decode(b64)
        whatfile = imghdr.what(None, img_data)
        #print(whatfile)

        # save to disk
        # set filename as yyyymmddhhmmssfff formatted date now
        #now = datetime.datetime.now()
        #filename = now.strftime("%Y%m%d%H%M%S%f") + '.' + whatfile
        #filename = secure_filename(filename)
        #filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        #with open(filepath, 'wb') as f:
        #    f.write(img_data)

        image = Image.open(io.BytesIO(img_data))
        w = image.width
        h = image.height
        scale = 800 / max(w, h)
        if(scale < 1):
            image = image.resize((int(w * scale), int(h * scale)))

        ret = ocrflorence2base.ocr(image, task_prompt)
        if '<OCR_WITH_REGION>' in ret:
            ret["image_width"] = image.width;
            ret["image_height"] = image.height;
        
        return ret
        
    return 

