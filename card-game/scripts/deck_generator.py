import argparse
import os
import json

import shutil
from datetime import datetime

from avatar_generator import format_image, generate_pixel_art_avatar

data_path = "../decks/generator/data.json"

raw_img_path = "../assets/sprites/avatars/raw/"
avatar_img_path = "../assets/sprites/avatars/"

godot_avatar_img_path = "res://assets/sprites/avatars/"

MAX_BACKUPS = 50


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


def save_data(data, backup=True, sort=False):
    if sort:
        data = {
            "nouns": { k: v for k, v in sorted(data["nouns"].items(), key=lambda x: (x[1]['level'], x[0])) },
            "adjectives": { k: v for k, v in sorted(data["adjectives"].items(), key=lambda x: (x[1]['level'], x[0])) }
        }

    if backup:
        backup_dir = os.path.join(os.environ['USERPROFILE'], 'Documents', 'Backups')
        os.makedirs(backup_dir, exist_ok=True)

        timestamp = datetime.now().strftime("%Y%m%d%H%M%S")
        backup_file = os.path.join(backup_dir, f"cardgame.data.{timestamp}.json")
        shutil.copy(data_path, backup_file)
        print(f"Backed up file to {backup_file}")

        backups = sorted([os.path.join(backup_dir, f) for f in os.listdir(backup_dir) if f.startswith('cardgame.data')], key=os.path.getctime)
        if len(backups) > MAX_BACKUPS:
            for old_backup in backups[:-MAX_BACKUPS]:
                os.remove(old_backup)
                print(f"Removed old backup: {old_backup}")

    with open(data_path, "w") as file:
        file.write(json.dumps(data, indent=3))


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


def generate_avatars(data: dict, creature_name: str, n: int) -> None:
    if creature_name not in data["nouns"]:
        print(f"ERROR: Unknown creature {args.creature}")
        return
    
    noun_data = data["nouns"][creature_name]
    if not noun_data.get("avatars"):
        noun_data["avatars"] = []

    current_n = len(noun_data["avatars"])
    new_n = n - current_n
    if new_n <= 0:
        return

    raw_img_paths = generate_pixel_art_avatar(creature_name, raw_img_path, new_n)
    for img_path in raw_img_paths:
        new_avatar_path = format_image(img_path, avatar_img_path)
        new_avatar_filename = os.path.basename(new_avatar_path)
        new_godot_avatar_path = godot_avatar_img_path + new_avatar_filename
        noun_data["avatars"].append(new_godot_avatar_path)


def remove_dangling_resources(data: dict) -> None:
    avatar_resources = set([godot_avatar_img_path + res for res in os.listdir(avatar_img_path) if res.endswith(".png") ])

    deck_resources = set()
    for info in data["nouns"].values():
        avatars = info.get("avatars")
        if avatars:
            deck_resources.update(avatars)
            removed_avatars = [ res for res in avatars if res not in avatar_resources ]
            avatars = [ res for res in avatars if res in avatar_resources ]

            if removed_avatars:
                print(f'Update: Removed resource from deck data: {removed_avatars}')
                info["avatars"] = avatars
    
    for res in avatar_resources:
        if res not in deck_resources:
            print(f"Warning: Resource that is not in any deck resource: {res}")


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

    avatars_command = subparser.add_parser("avatars", help="Generate avatars for creatures.")
    avatars_command.add_argument("-n", type=int, default=4, help="How many avatars to generate. (Be careful setting above 4....)")
    avatars_command.add_argument("--creature", default=None, help="Generate for a single creature/noun.")

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
    elif args.command == "avatars":
        if args.creature:
            generate_avatars(data, args.creature, args.n)
            save_data(data, sort=True)
        else:
            for noun in data["nouns"].keys():
                generate_avatars(data, noun, args.n)
                save_data(data, sort=True)
    elif args.command == "clean":
        remove_dangling_resources(data)
        save_data(data, sort=True)
    else:
        parser.print_help()
