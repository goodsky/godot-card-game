from dotenv import load_dotenv
from openai import OpenAI
from PIL import Image
import os
import base64

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


def generate_image(prompt: str, output_dir: str, filename: str) -> list[str]:
    print(f'Generating image for "{prompt}"')

    if not filename.endswith(".png"):
        filename = filename + ".png"

    response = client.images.generate(
        model="dall-e-2",
        prompt=prompt,
        response_format="b64_json",
        size="256x256",
        n=4,
    )

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
    prompt = "Video game avatar of a common squirrel. GameBoy low-resolution pixel art."
    filename = "squirrel"

    raw_dir = "../assets/sprites/avatars/raw"
    avatar_dir = "../assets/sprites/avatars"
    img_paths = generate_image(prompt, raw_dir, filename)
    for img_path in img_paths:
        format_image(img_path, avatar_dir)
