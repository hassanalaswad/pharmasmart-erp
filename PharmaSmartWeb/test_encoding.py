import os

corrupt = "ط§ظ„"
try:
    b = corrupt.encode('windows-1252', errors='replace')
    print("1252:", b.decode('utf-8', errors='replace'))
except Exception as e:
    pass

try:
    b = corrupt.encode('windows-1256', errors='replace')
    print("1256:", b.decode('utf-8', errors='replace'))
except Exception as e:
    pass

try:
    b = corrupt.encode('latin1', errors='replace')
    print("latin1:", b.decode('utf-8', errors='replace'))
except Exception as e:
    pass

# We can also test reading a real file and see what decodes.
with open("Views/Home/Index.cshtml", "r", encoding="utf-8") as f:
    text = f.read()
    
# Find a corrupted word
target = "ط§ظ„"
if target in text:
    print(f"Found corrupted text in Index.cshtml")
    # try reverting mapping
    # Latin-1 is actually exactly mapping bytes to 0-255 characters.
    # If UTF-8 bytes were read as windows-1256, it mapped [0xE2, 0x80, ...] to cp1256 chars.
    
    # Let's map it via windows-1256 encode -> utf-8 decode
    b = target.encode('windows-1256', errors='replace')
    print("target cp1256->utf-8:", b.decode('utf-8', errors='replace'))
    
    b2 = target.encode('windows-1252', errors='replace')
    print("target cp1252->utf-8:", b2.decode('utf-8', errors='replace'))
    
    # What if it's utf-8 decoded as windows-1252?
    print("target latin1->utf-8:", target.encode('latin1', errors='replace').decode('utf-8', errors='replace'))

