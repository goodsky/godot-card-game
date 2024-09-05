shader_type canvas_item;

void fragment() {
	vec2 newUV = UV;
	newUV.x += TIME * 0.25;
	newUV.y += TIME * 0.1;
	COLOR = texture(TEXTURE, newUV);
}
