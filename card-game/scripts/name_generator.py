from dotenv import load_dotenv
from openai import OpenAI
import os
import json

load_dotenv()
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
client = OpenAI(api_key=OPENAI_API_KEY)

prompt_path = "./name_generator.prompt.txt"
data_path = "../decks/generator/data.json"


def get_assistant_response(messages):
    response = client.chat.completions.create(
        model="gpt-4o",
        response_format={"type": "json_object"},
        temperature=1.0,
        messages=messages,
    )

    return response.choices[0].message


def get_json_list_from_message(assistant_message) -> list[str]:
    content = assistant_message.content
    content_json = json.loads(content)
    content_keys = list(content_json.keys())
    if len(content_keys) != 1:
        print(
            f"WARNING: unexpected JSON response from model with more than one key. OUTPUT:\n{content}"
        )

    return content_json[content_keys[0]]


def generate_lists() -> dict:
    with open(prompt_path, "r") as file:
        nouns_prompt = file.read()

    levels = ["NOOB LEVEL", "LOW LEVEL", "MEDIUM LEVEL", "HIGH LEVEL", "EPIC LEVEL"]
    list_types = ["nouns", "adjectives"]
    data = {x: {} for x in list_types}

    messages = [{"role": "system", "content": nouns_prompt}]

    for level in levels:
        for list_type in list_types:
            messages.append(
                {
                    "role": "user",
                    "content": f"Provide the list of {list_type} for {level} creatures.",
                }
            )

            print(f"Generating list of {list_type} for {level} creatures.")
            assistant_response = get_assistant_response(messages)
            messages.append(assistant_response)

            data[list_type][level] = get_json_list_from_message(assistant_response)

    print(json.dumps(data, indent=3))
    return data


if __name__ == "__main__":
    data = generate_lists()
    with open(data_path, "w") as file:
        file.write(json.dumps(data, indent=3))
    print(f'Wrote data to "{data_path}"')
