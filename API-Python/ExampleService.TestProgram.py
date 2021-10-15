import MicroServerAPI

serv = MicroServerAPI.MicroService(url_get='http://localhost:8080/microserver/get-job',
                                   url_post='http://localhost:8080/microserver/post-job',
                                   persistentMode=True)

while True:
    st = []
    inp = input('Enter string: ')
    while inp != '':
        st.append(inp)
        inp = input('Enter next string (leave blank to exit): ')

    print("Input array:")
    print(st)
    print()
    print("Requesting concatenation...")

    result = serv.ProcessAsFunction(content=st,
                                    requested_function='ArrayConcatenator.A42.ConcatenateStringArrayToString',
                                    target_content_type='ArrayConcatenator.A42.ConcatenateStringArrayToString.Result')

    print("Result: ")
    print(result)
    print()
