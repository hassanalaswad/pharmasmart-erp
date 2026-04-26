import sys
import re

def fix_file(f):
    try:
        t = open(f, encoding='utf-8').read()
        # Add @ViewBag.CurrencySymbol after ToString("N2") if not already there
        # Also handle "N2" inside ToString like ToString("N2")
        t = re.sub(r'ToString\("N2"\)(?! @ViewBag.CurrencySymbol| @Settings.Currency|<)', r'ToString("N2") @ViewBag.CurrencySymbol', t)
        
        # Replace remaining Settings.Currency
        t = t.replace('@Settings.Currency', '@ViewBag.CurrencySymbol')
        
        open(f, 'w', encoding='utf-8').write(t)
        print(f"Fixed {f}")
    except Exception as e:
        print(f"Error {f}: {e}")

if __name__ == "__main__":
    for arg in sys.argv[1:]:
        fix_file(arg)
