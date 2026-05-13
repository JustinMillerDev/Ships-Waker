shader_type canvas_item;

/// Width of each health segment in pixels.
uniform float notch_size : hint_range(1.0, 32.0, 1.0) = 4.0;
/// Thickness of the black divider line in pixels.
uniform float notch_thickness : hint_range(0.5, 4.0, 0.5) = 1.0;
/// Colour of the divider lines.
uniform vec4 notch_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

void fragment() {
	// Sample the underlying TextureProgressBar output.
	vec4 base = texture(TEXTURE, UV);

	// Convert UV to pixel coordinates along the X axis.
	float texture_width = 1.0 / TEXTURE_PIXEL_SIZE.x;
	float pixel_x = UV.x / TEXTURE_PIXEL_SIZE.x;

	// Offset by 1 pixel so the first notch starts at x=1 instead of x=0.
	// Also suppress any notch on the very last pixel column.
	float shifted_x = pixel_x - 1.0;
	float pos_in_segment = mod(shifted_x, notch_size);
	bool is_last_pixel = pixel_x >= (texture_width - 1.0);

	// Suppress notches on the top and bottom 1-pixel border rows.
	float texture_height = 1.0 / TEXTURE_PIXEL_SIZE.y;
	float pixel_y = UV.y / TEXTURE_PIXEL_SIZE.y;
	bool is_border_row = pixel_y < 1.0 || pixel_y >= (texture_height - 1.0);

	// Draw the divider for the first `notch_thickness` pixels of every segment,
	// but only where the bar is opaque, not on the final pixel, and not on border rows.
	bool is_notch = (shifted_x >= 0.0) && (pos_in_segment < notch_thickness) && (base.a >= 0.01) && !is_last_pixel && !is_border_row;
	COLOR = is_notch ? vec4(notch_color.rgb, base.a * notch_color.a) : base;
}
