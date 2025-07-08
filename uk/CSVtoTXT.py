# Назвы файлаў
input_file = "localization-resources.csv"
output_file = "converted_localization.txt"

# Чытанне зместу
with open(input_file, "r", encoding="utf-8") as infile:
    lines = infile.readlines()

# Замена першых трох радкоў
if len(lines) >= 3:
    lines[0] = '0 TextAsset Base\n'
    lines[1] = ' 1 string m_Name = "localization"\n'
    lines[2] = ' 1 string m_Script = """\n'

# Аб’яднанне ў адзіны радок і замена ўсіх новых радкоў на \n
content = ''.join(lines)
converted = content.replace('\n', '\\n')

# Зваротная замена для тых першых трох, каб яны былі як трэба (без \\n)
converted = converted.replace('0 TextAsset Base\\n', '0 TextAsset Base\n', 1)
converted = converted.replace(' 1 string m_Name = "localization"\\n', ' 1 string m_Name = "localization"\n', 1)
converted = converted.replace(' 1 string m_Script = """\\n', ' 1 string m_Script = """\n', 1)

# Дадаем закрываючы """ у канцы
converted += '\n"""'

# Запіс у файл
with open(output_file, "w", encoding="utf-8") as outfile:
    outfile.write(converted)

print("Гатова! Захавана ў", output_file)
