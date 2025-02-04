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
#from werkzeug.exceptions import HTTPException
# pip freeze > requirements.txt
# pip install -r requirements.txt
import CLIP_processor

#UPLOAD_FOLDER = '/path/to/the/uploads'
UPLOAD_FOLDER = 'c:\\temp\\flask_test\\uploads'
ALLOWED_EXTENSIONS = {'txt', 'pdf', 'png', 'jpg', 'jpeg', 'gif'}

app = Flask(__name__)
#app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER


def allowed_file(filename):
    return '.' in filename and \
           filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


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


@app.route('/ischecked', methods=['POST'])
@app.route('/isChecked', methods=['POST'])
@app.route('/IsChecked', methods=['POST'])
def ischecked():
    if request.method == 'POST':        
        if request.is_json:
            #print(request.json)
            b64 = request.json['b64']
            if b64 != None:
                img_data = base64.b64decode(b64)
                whatfile = imghdr.what(None, img_data)
                print(whatfile)
                if('UPLOAD_FOLDER' in app.config):
                    # set filename as yyyymmddhhmmssfff formatted date now
                    now = datetime.datetime.now()
                    filename = now.strftime("%Y%m%d%H%M%S%f") + '.' + whatfile
                    filename = secure_filename(filename)
                    filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
                    with open(filepath, 'wb') as f:
                        f.write(img_data)
                image = Image.open(io.BytesIO(img_data))
                ret = CLIP_processor.readCheckboxImage(image)
                print("ischecked return: " + str(ret))
                return str(ret)

        # check if the post request has the file part
        if 'file' not in request.files:
            raise UnknownError("'file' not in request.files")
        
        file = request.files['file']
        # If the user does not select a file, the browser submits an
        # empty file without a filename.        
        if file == None or file.filename == '':
            raise UnknownError('No selected file')
        
        if file and allowed_file(file.filename):
            if('UPLOAD_FOLDER' in app.config):
                # set filename as yyyymmddhhmmssfff formatted date now
                now = datetime.datetime.now()
                filename = now.strftime("%Y%m%d%H%M%S%f") + '.' + whatfile
                filename = secure_filename(filename)
                filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
                file.save(filepath)
            #image = Image.open(filepath)
            image = Image.open(file)
            ret = CLIP_processor.readCheckboxImage(image)
            print("ischecked return: " + str(ret))
            return str(ret)
        
    return 
