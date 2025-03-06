# Deck Asset Generator Scripts

This folder contains Python scripts to generate new deck assets.

## Installing SDXL for Avatar Generation

The `avatar_generation_local.py` script expects pytorch with CUDA 12.6. When installing dependencies, use `pip install -r requirements.txt --index-url https://download.pytorch.org/whl/cu126`


*Sample: Generate a single new avatar* `> python avatar_generator_local.py "Goblin"`


*Sample: Regenerate avatars for game* `> python deck_generator.py avatars`
   * Note: this script will use ../settings/cards.data.json as the reference for creature names. It will only generate avatars if a creature noun has fewer than n=4 images. You can delete images and then run `> python deck_generator.py clean` to make room for new avatars or add new creature nouns to the JSON file.