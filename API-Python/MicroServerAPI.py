import requests
import json
import urllib
import datetime
import __main__

class ResponseAddress:
    """Response address for posting result"""
    id:str = ''
    visibleId:str=None

    def __init__(self, id:str, visibleId:str):
        self.id = id;
        self.visibleId = visibleId;


class MicroService:
    """API for client of MicroServer"""
    
    __HEADER_CONTENT_SEND = {'content-type': 'application/json'}
    __HEADER_CONTENT_ACCEPT = {'accept': 'application/json'}

    __url_get:str = ''
    __url_post:str = ''

    __persistentMode:bool = False

    def __init__(self, url_get:str, url_post:str, persistentMode:bool = False):
        """If 'persistentMode' set to 'true', functions will try to connect to the server over and over again, even if it is not available. The default value is 'false'"""
        try:
            result = urllib.parse.urlparse(url_get)
            assert(all([result.scheme, result.netloc]) == True)
        except:
            raise Exception("'url_get' contains invalid url!")

        try:
            result = urllib.parse.urlparse(url_post)
            assert(all([result.scheme, result.netloc]) == True)
        except:
            raise Exception("'url_post' contains invalid url!")

        self.__url_get = url_get
        self.__url_post = url_post
        self.__persistentMode = persistentMode

    @property
    def PersistentMode(self):
        return self.__persistentMode
    @PersistentMode.setter
    def PersistentMode(self, is_active:bool):
        self.__persistentMode = is_active

    def GetJob(self, job_type:str='null', job_id:str='null'):
        if job_type == 'null' and job_id == 'null':
            raise Exception("'job_type' and 'job_id' can not be null at the same time!")

        response = None
        while True:
            try:
                response = requests.get(self.__url_get + '?type=' + urllib.parse.quote_plus(job_type) + '&id=' + urllib.parse.quote_plus(job_id), timeout=60, headers=self.__HEADER_CONTENT_ACCEPT)
                if response.status_code == 408:
                    continue;
                break;
            except requests.exceptions.ReadTimeout:
                continue;
            except:
                if self.__persistentMode:
                    continue;
                else:
                    raise

        if response.status_code < 200 or response.status_code >= 300:
             raise Exception("The received status code does not meet the conditions for normal operation!")

        basic_content = json.loads(response.text)
        return basic_content['content'], ResponseAddress(basic_content['id'], basic_content['visibleId']) 
    
    def GetNextJob(self, job_type:str):
        if job_type == 'null':
            raise Exception("'job_type' can not be null!")

        return self.GetJob(job_type=job_type)
        

    def PostJob(self, content, job_type:str='null', job_id:str='null', visibleId:bool=True):
        if job_type == 'null' and not visibleId:
            raise Exception("'job_type' is 'null' and 'visibleId' is 'False' at the same time!")

        basic_content = json.dumps({'id':job_id, 
                                    'visibleId':visibleId,
                                    'type':job_type,
                                    'content': content }, separators=(',', ':'))
        response = None
        while True:
            try:
                response = requests.post(self.__url_post, headers=self.__HEADER_CONTENT_SEND, data=basic_content)
                break;
            except:
                if not self.__persistentMode:
                    raise
        
        if response.status_code < 200 or response.status_code >= 300:
            raise Exception("The received status code does not meet the conditions for normal operation!")

    def PostFinalResult(self, content, responseAddress:ResponseAddress, result_type:str = 'null'):
        self.PostJob(content=content, job_type=result_type, job_id=responseAddress.id, visibleId=True)
        
    def PostIntermediateResult(self, content, responseAddress:ResponseAddress, result_type:str):
        if result_type == 'null':
            raise Exception("'result_type' can not be 'null' in intermediate result!")
        
        self.PostJob(content=content, job_type=result_type, job_id=responseAddress.id, visibleId=responseAddress.visibleId)

    def ProcessAsFunction(self, content, requested_function:str, target_content_type:str='null'):
        if requested_function == 'null':
            raise Exception("'requested_function' can not be 'null'!")

        cid = __main__.__file__ + '.' + str(datetime.datetime.utcnow()) 
        
        self.PostJob(content=content, job_type=requested_function, job_id=cid, visibleId=False)
        return self.GetJob(job_type=target_content_type, job_id=cid)[0]
