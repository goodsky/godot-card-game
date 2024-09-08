import random

adjectives_file = "../decks/generator_adj.txt"
nouns_file = "../decks/generator_noun.txt"

def dedupe_and_sort(file_name) -> list[str]:
    with open(file_name, 'r') as file:
        items = file.read().splitlines()
        unique_items = list(set(items))
        unique_items.sort()

    with open(file_name, 'w') as file:
        file.write('\n'.join(unique_items))

    return unique_items


def main():
    adjectives = dedupe_and_sort(adjectives_file)
    nouns = dedupe_and_sort(nouns_file)

    for _ in range(100):
        adj = random.choice(adjectives)
        noun = random.choice(nouns)
        print(f'{adj} {noun}')

if __name__ == "__main__":
    main()