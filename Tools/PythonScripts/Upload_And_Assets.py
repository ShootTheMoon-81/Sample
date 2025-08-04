from __future__ import print_function

import boto3
import os
import os.path
import time
import glob
import mimetypes
import requests
import json

from botocore.client import Config
from google.auth.transport.requests import Request
from google.oauth2.credentials import Credentials
from google_auth_oauthlib.flow import InstalledAppFlow
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
from googleapiclient.http import MediaFileUpload

# 빌드파일이 저장되는 경로.
BuildFiles_Path = '../Build/Android'

# 업로드 파일 링크할 슬랙 채널.
Alarm_Slack_Channel = '#빌드'

# S3 정보
ACCESS_KEY_ID = '*' #s3 관련 권한을 가진 IAM계정 정보
ACCESS_SECRET_KEY = '*'
BUCKET_NAME = 'patch'
# Android 만 동작함.(iOS는 경로 수정 필요)
DEST_FOLDER = 'assets/Android/'
FOLDER_PATH = '../../ServerData/Android'
# Version 파일 관련.
BUNDLE_VERSION_DEST_FOLDER = 'assets/BundleVersion/'
BUNDLE_VERSION_FOLDER_PATH = '../../ServerData/BundleVersion'


def main():

    # cdn 업로드. 릴리즈는 googledrive에 올리지 않고 있음...적용여부는 상의해야될듯
    handle_upload()

    slack_message(Alarm_Slack_Channel, "assets CDN UpLoad")
    
    
#S3 업로드
def handle_upload():
    print('Start S3 Upload')
    s3 = boto3.client('s3',
                  aws_access_key_id=ACCESS_KEY_ID,
                  aws_secret_access_key=ACCESS_SECRET_KEY)
    
# 폴더 내의 모든 파일과 하위 폴더를 순회하며 업로드합니다
    for root, dirs, files in os.walk(FOLDER_PATH):
        for file in files:
            file_path = root + '/' + file
            # S3에 업로드할 경로를 구성합니다      
            s3_key = DEST_FOLDER + os.path.relpath(file_path, FOLDER_PATH).replace('\\','/')
        
            try:
                # 파일을 업로드합니다
                s3.upload_file(file_path, BUCKET_NAME, s3_key)
                print(s3_key + ' upload success')
            except Exception as e:
                print(s3_key +  '업로드 중 오류 발생') 

        for directory in dirs:
            dir_path = root + '/' + directory
            s3_key = DEST_FOLDER + os.path.relpath(dir_path, FOLDER_PATH).replace('\\','/') + '/'
        
            try:
                # 폴더를 생성합니다
                s3.put_object(Bucket=BUCKET_NAME, Key=s3_key)
                print(s3_key + ' create floder')
            except Exception as e:  
                print(s3_key + ' 폴더 생성 중 오류 발생')

    # Bundle_Version 파일. 코드 다음어야 함. 우선 안전하게.
    for root, dirs, files in os.walk(BUNDLE_VERSION_FOLDER_PATH):
        for file in files:
            file_path = root + '/' + file
            # S3에 업로드할 경로를 구성합니다
            s3_key = BUNDLE_VERSION_DEST_FOLDER + os.path.relpath(file_path, BUNDLE_VERSION_FOLDER_PATH).replace('\\', '/')

            try:
                # Bundle_Version 파일을 업로드합니다
                s3.upload_file(file_path, BUCKET_NAME, s3_key)
                print(s3_key + ' version file upload success')
            except Exception as e:
                print(s3_key + '버전 파일 업로드 중 오류 발생')

    print('Finish S3 Upload')

# Slack에 메세지 보내기. Http 전송으로 간단하게.
def slack_message(channel, message):
    # macovill Bot app 토큰.
    slack_bot_token = "*"
    headers = {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + slack_bot_token
    }
    payload = {
        'channel': channel,
        'text': message
    }
    requests.post('https://slack.com/api/chat.postMessage',
                  headers=headers,
                  data=json.dumps(payload)
                  )


if __name__ == '__main__':
    main()

