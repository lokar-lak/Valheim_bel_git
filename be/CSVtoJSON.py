import pandas as pd
import json

# Увод і вывад
input_csv = "localization-resources.csv"
output_json = "localization.json"

# Чытанне CSV
df = pd.read_csv(input_csv)

# Захаванне толькі першага і апошняга слупка
df_reduced = df.iloc[:, [0, -1]]

# Фільтрацыя: выдаляем радкі з пустымі ключамі або значэннямі
df_filtered = df_reduced.dropna()

# Прапуск канкрэтных ключоў
excluded_keys = {
    ' 1 string m_Name = "localization"',
    ' 1 string m_Script = """'
}

df_filtered = df_filtered[~df_filtered.iloc[:, 0].isin(excluded_keys)]

# Канвертацыя ў слоўнік
json_data = dict(zip(df_filtered.iloc[:, 0], df_filtered.iloc[:, 1]))

# Запіс у JSON
with open(output_json, "w", encoding="utf-8") as f:
    json.dump(json_data, f, ensure_ascii=False, indent=2)

print(f"JSON захаваны як {output_json}")
