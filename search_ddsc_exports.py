import os
import subprocess
import re
import sys

def check_exports(dll_path, search_terms):
    print(f"Checking exports for: {dll_path}")
    
    if not os.path.exists(dll_path):
        print(f"Error: File not found: {dll_path}")
        return

    found_any = False

    # Method 1: Try using dumpbin
    try:
        result = subprocess.run(['dumpbin', '/EXPORTS', dll_path], capture_output=True, text=True)
        if result.returncode == 0:
            print("Successfully ran dumpbin.")
            lines = result.stdout.splitlines()
            for line in lines:
                # dumpbin output format typically: ordinal hint RVA      name
                # We want the name column.
                parts = line.split()
                if len(parts) >= 4:
                    symbol = parts[-1] # Usually the last part is the name, or if (forwarded) it might be different, but works for most.
                    if any(term in symbol for term in search_terms):
                         print(f"Match found (dumpbin): {symbol}")
                         found_any = True
            if found_any:
                return
            else:
                 print("dumpbin ran but no matches found. Checking binary content just in case...")

        else:
            print("dumpbin returned non-zero exit code or not found. Falling back into binary regex search.")
    except FileNotFoundError:
        print("dumpbin not found in PATH. Falling back to binary regex search.")
    except Exception as e:
        print(f"Error running dumpbin: {e}. Falling back into binary regex search.")

    # Method 2: Regex search on binary content
    print("Performing binary string search...")
    try:
        with open(dll_path, 'rb') as f:
            content = f.read()
            
        # We are looking for C-style strings in the binary that look like function names.
        # A simple pattern: alphanumeric + underscores, containing our search terms.
        # We'll search for each term.
        
        # Helper to extract strings
        def strings(file_content, min_len=4):
            result = ""
            for byte in file_content:
                if 32 <= byte <= 126: # Printable ASCII
                    result += chr(byte)
                else:
                    if len(result) >= min_len:
                        yield result
                    result = ""
            if len(result) >= min_len:
                yield result

        found_strings = set()
        for s in strings(content):
            if any(term in s for term in search_terms):
                # Heuristic: function names usually don't have spaces and often start with dds or similar
                # but valid C symbols are letters, numbers, underscores.
                if re.match(r'^[a-zA-Z0-9_]+$', s): 
                     found_strings.add(s)

        for s in sorted(found_strings):
             print(f"Match found (binary strings): {s}")

    except Exception as e:
        print(f"Error reading file: {e}")

if __name__ == "__main__":
    dll_path = r"d:\Work\FastCycloneDdsCsharpBindings\cyclone-compiled\bin\ddsc.dll"
    # User's search terms
    terms = ["write", "create_serdata", "serdata_from"]
    check_exports(dll_path, terms)
