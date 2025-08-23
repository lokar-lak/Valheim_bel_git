# Назвы файлаў
input_file = "localization-resources.csv"
output_file = "converted_localization.txt"

# Чытанне зместу
with open(input_file, "r", encoding="utf-8") as infile:
    lines = infile.readlines()

# Замена першых трох радкоў
if len(lines) >= 3:
    lines[0] = '0 TextAsset Base
'
    lines[1] = ' 1 string m_Name = "localization"
'
    lines[2] = ' 1 string m_Script = """
'

# Аб’яднанне ў адзіны радок і замена ўсіх новых радкоў на 

content = ''.join(lines)
converted = content.replace('
', '\
')

# Зваротная замена для тых першых трох, каб яны былі як трэба (без \
)
converted = converted.replace('0 TextAsset Base\
', '0 TextAsset Base
', 1)
converted = converted.replace(' 1 string m_Name = "localization"\
', ' 1 string m_Name = "localization"
', 1)
converted = converted.replace(' 1 string m_Script = """\
', ' 1 string m_Script = """
', 1)

# Дадаем закрываючы """ у канцы
converted += '
"""'

# Запіс у файл
with open(output_file, "w", encoding="utf-8") as outfile:
    outfile.write(converted)

print("Гатова! Захавана ў", output_file)
