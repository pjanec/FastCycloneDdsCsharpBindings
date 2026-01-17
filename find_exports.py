import re

dll_path = r"D:\Work\FastCycloneDdsCsharpBindings\cyclone-compiled\bin\ddsc.dll"

with open(dll_path, 'rb') as f:
    content = f.read()
    # Regex for null-terminated strings starting with dds_
    matches = re.finditer(b'dds_[a-zA-Z0-9_]+', content)
    
    found = set()
    for m in matches:
        try:
            s = m.group().decode('ascii')
            if s.startswith('ddsi_'):
                found.add(s)
        except:
            pass
            
    print("Found symbols:")
    for s in sorted(found):
        print(s)
