import time, urllib.request
t = time.time()
while time.time() - t < 900:
    try:
        urllib.request.urlopen('http://127.0.0.1:8001/health', timeout=5)
        print('api up after %.0fs' % (time.time() - t)); break
    except Exception:
        time.sleep(5)
else:
    print('api timeout')
