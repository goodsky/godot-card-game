from dotenv import load_dotenv
from openai import OpenAI, RateLimitError
from PIL import Image
import argparse
import os
import base64
import time

###
# THIS SCRIPT IS DEPRECATED
# Use avatar_generator_local.py instead to use the local model.
###

load_dotenv()
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
client = OpenAI(api_key=OPENAI_API_KEY)


def get_unique_path(path: str) -> str:
    root, ext = os.path.splitext(path)
    num = 0
    path = f"{root}_{num}{ext}"
    while os.path.exists(path):
        path = f"{root}_{num}{ext}"
        num += 1
    return path


def format_name_for_filepath(name: str) -> str:
    filename = name.lower()
    filename = filename.replace(' ', '_')
    return filename + ".png"


def generate_pixel_art_avatar(creature: str, output_dir: str, n: int = 4) -> list[str]:
    filename = format_name_for_filepath(creature)
    prompt = f"Cute video game avatar of a {creature} showing the full body. GameBoy low-resolution pixel art."

    return generate_image(prompt, output_dir, filename, n)


def generate_image(prompt: str, output_dir: str, filename: str, n: int = 4) -> list[str]:
    print(f'Generating image for "{prompt}"')

    if not filename.endswith(".png"):
        filename = filename + ".png"

    RETRY_COUNT = 3
    retry = 0
    while retry < RETRY_COUNT:
        try:
            response = client.images.generate(
                model="dall-e-2",
                prompt=prompt,
                response_format="b64_json",
                size="256x256",
                n=n,
            )
            break
        except RateLimitError as e:
            print("Rate Limit Exceeded! Waiting 60 seconds and then trying again.")
            retry += 1
            if retry >= RETRY_COUNT:
                raise e
            time.sleep(60)


    image_paths = []
    for img_data in response.data:
        b64_str = img_data.b64_json
        image_data = base64.b64decode(b64_str)

        image_path = get_unique_path(os.path.join(output_dir, filename))
        with open(image_path, "wb") as file:
            file.write(image_data)

        image_paths.append(image_path)
        print(f'Saved image to "{image_path}"')

    return image_paths


def format_image(image_path: str, output_dir: str) -> str:
    rescale_size = (60, 60)
    avatar_size = (90, 60)
    with Image.open(image_path) as img:
        colors = img.getcolors(maxcolors=256 * 256)
        background_color = max(colors, key=lambda item: item[0])[1]

        resized_img = img.resize(rescale_size)

        avatar_img = Image.new("RGB", avatar_size, background_color)
        avatar_img.paste(resized_img, (15, 0))

        filename = os.path.basename(image_path)
        root, ext = os.path.splitext(filename)
        avatar_path = os.path.join(output_dir, f"avatar_{root}{ext}")
        avatar_img.save(avatar_path)

        print(f'Saved modified image to "{avatar_path}"')
        return avatar_path


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate card game avatars")
    parser.add_argument("creature", type=str)
    args = parser.parse_args()

    raw_img_dir = "../assets/sprites/avatars/raw"
    avatar_img_dir = "../assets/sprites/avatars"
    img_paths = generate_pixel_art_avatar(args.creature)
    for img_path in img_paths:
        format_image(img_path, avatar_img_dir)
