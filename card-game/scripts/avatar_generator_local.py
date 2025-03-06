import argparse
import os
import torch
from PIL import Image
from diffusers import StableDiffusionXLPipeline, EulerDiscreteScheduler
from huggingface_hub import hf_hub_download
from pixel_scaler import scale_image

prompt_path = "./avatar.prompt.txt"
negative_prompt_path = "./avatar.negative.prompt.txt"

parser = argparse.ArgumentParser(description="Generate card game avatars")
parser.add_argument("creature", type=str)
args = parser.parse_args()


def generate_pixel_art_avatar(creature: str, output_dir: str, pipeline: StableDiffusionXLPipeline = None, n: int = 4) -> list[str]:
    filename = format_name_for_filepath(creature)
    with open(prompt_path, "r") as file:
        prompt = file.read()
    prompt = prompt.replace("{name}", creature)
    
    with open(negative_prompt_path, "r") as file:
        negative_prompt = file.read()

    if pipeline is None:
        pipeline = initialize_diffusion_pipeline()

    return generate_image(prompt, negative_prompt, output_dir, filename, pipeline, n)


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


def initialize_diffusion_pipeline() -> StableDiffusionXLPipeline:
    base = "stabilityai/stable-diffusion-xl-base-1.0"
    lightning_lora = "ByteDance/SDXL-Lightning"
    lightning_4step_checkpoint = "sdxl_lightning_4step_lora.safetensors"

    pixel_lora = "nerijs/pixel-art-xl"

    # Load model + LORA weights
    pipe = StableDiffusionXLPipeline.from_pretrained(base, torch_dtype=torch.float16, variant="fp16")
    pipe.load_lora_weights(hf_hub_download(lightning_lora, lightning_4step_checkpoint), adapter_name="lightning")
    pipe.load_lora_weights(pixel_lora, adapter_name="pixel")
    pipe.set_adapters(["lightning", "pixel"], adapter_weights=[1.0, 1.2])
    pipe.fuse_lora()

    # Ensure sampler uses "trailing" timesteps
    pipe.scheduler = EulerDiscreteScheduler.from_config(pipe.scheduler.config, timestep_spacing="trailing")
    pipe.enable_model_cpu_offload()
    return pipe


def generate_image(prompt: str, negative_prompt: str, output_dir: str, filename: str, pipeline: StableDiffusionXLPipeline, n: int = 4) -> list[str]:
    os.makedirs(output_dir, exist_ok=True)

    print(f'Generating avatar:')
    print(f"   Prompt: {prompt}")
    print(f"   Negative Prompt: {negative_prompt}")
    print(f"   Output Directory: {output_dir}")
    print(f"   Filename: {filename}")

    image_paths = []
    for i in range(n):
        print(f"    Image Generation Progress: {i + 1}/{n}")
        img = pipeline(
            prompt=prompt,
            negative_prompt=negative_prompt,
            num_inference_steps=4,
            guidance_scale=1.5,
            width=1216,
            height=832).images[0]
        
        img_path = get_unique_path(os.path.join(output_dir, filename))
        img.save(img_path)
        image_paths.append(img_path)

    return image_paths


def format_image(image_path: str, output_dir: str) -> str:
    avatar_size = (90, 60)

    filename = os.path.basename(image_path)
    avatar_path = os.path.join(output_dir, f"avatar_{filename}")
    scale_image(image_path, avatar_path, avatar_size[1], avatar_size[0])
    print(f'Saved modified image to "{avatar_path}"')
    return avatar_path


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate card game avatars")
    parser.add_argument("creature", type=str)
    args = parser.parse_args()

    raw_img_dir = "img/raw"
    avatar_img_dir = "img"
    img_paths = generate_pixel_art_avatar(args.creature, raw_img_dir)
    for img_path in img_paths:
        format_image(img_path, avatar_img_dir)
