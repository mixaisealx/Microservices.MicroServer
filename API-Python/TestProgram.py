import MicroServerAPI

serv = MicroServerAPI.MicroService('http://localhost:8080/microserver/get-job','http://localhost:8080/microserver/post-job')

while True:
    data,addr = serv.GetNextJob(job_type='PyTest.01')

    print(data)

    serv.PostFinalResult(content='ok',job_type='PyTest.01.Result',responseAddress=addr)
