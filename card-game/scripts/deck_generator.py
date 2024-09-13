import argparse
import os
import json

from avatar_generator import generate_pixel_art_avatar

data_path = "../decks/generator/data.json"

raw_img_dir = "../assets/sprites/avatars/raw"
avatar_img_dir = "../assets/sprites/avatars"


def load_data() -> dict:
    with open(data_path, "r") as file:
        return json.loads(file.read())


def load_array(value: str) -> list[str]:
    if value.endswith(".json"):
        with open(value, "r") as file:
            return json.loads(file.read())
        
    if value.endswith(".txt"):
        with open(value, "r") as file:
            return [ line.strip() for line in file.readlines() ]
        
    return [ value ] 


def print_data(data: dict, details=False):
    nouns_by_level = {}
    for noun, info in data["nouns"].items():
        level = info["level"]
        if not level in nouns_by_level:
            nouns_by_level[level] = []
        nouns_by_level[level].append(noun)

    adj_by_level = {}
    for adj, info in data["adjectives"].items():
        level = info["level"]
        if not level in adj_by_level:
            adj_by_level[level] = []
        adj_by_level[level].append(adj)
    
    print("NOUNS:")
    for level, nouns in nouns_by_level.items():
        print(f"   Level {level}: {len(nouns)}")
    print(json.dumps(nouns_by_level, indent=3))
    print("\nADJECTIVES")
    for level, adjs in adj_by_level.items():
        print(f"   Level {level}: {len(adjs)}")
    print(json.dumps(adj_by_level, indent=3))


def save_data(data, sort=False):
    if sort:
        data = {
            "nouns": { k: v for k, v in sorted(data["nouns"].items(), key=lambda x: (x[1]['level'], x[0])) },
            "adjectives": { k: v for k, v in sorted(data["adjectives"].items(), key=lambda x: (x[1]['level'], x[0])) }
        }
        
    with open(data_path, "w") as file:
        file.write(json.dumps(data, indent=3))


def add_data(data: dict, type: str, values: list[str], level: int, overwrite=False):
    data = data[type]
    for value in values:
        if value in data:
            print(f"Value already exists! Value={value}. Existing={json.dumps(data[value])}")
            if overwrite:
                data[value]["level"] = int(level)
            continue

        data[value] = { "level": int(level) }
        print(f'Added {value} to the {type} list.')


def remove_dangling_resources(data: dict):
    for info in data["nouns"].values():
        print()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Utility for card game deck generator assets.")
    subparser = parser.add_subparsers(dest="command", help="Command to run.")

    print_command = subparser.add_parser("print", help="Pretty print the current deck generator assets.")

    add_command = subparser.add_parser("add", help="Add new nouns or adjectives to the current deck generator assets.")
    add_command.add_argument("type", choices=["noun", "adj", "adjective" ], help="Type of resource to add.")
    add_command.add_argument("value", help="The value to add or a .json file for a batch.")
    add_command.add_argument("--level", default=0, help="The level of the value to add")
    add_command.add_argument("--force", action="store_true", help="Overwrite existing values if they already exist")

    clean_command = subparser.add_parser("clean", help="Sort elements, remove dangling assets, reformat.")

    args = parser.parse_args()

    type_map = {
        "noun": "nouns",
        "adj": "adjectives",
        "adjective": "adjectives"
    }

    data = load_data()
    if args.command == "print":
        print_data(data)
    elif args.command == "add":
        data_type = type_map[args.type]
        values = load_array(args.value)
        add_data(data, data_type, values, args.level, overwrite=args.force)
        save_data(data, sort=True)
    elif args.command == "clean":
        save_data(data, sort=True)
    else:
        parser.print_help()
