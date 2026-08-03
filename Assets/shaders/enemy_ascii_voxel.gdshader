shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque;

// Per-face fake lighting: voxel faces have no scene-light dependency so the
// ascii glyph density reads the same regardless of world lighting setup.
uniform vec3 light_direction = vec3(0.4, 0.85, 0.3);
uniform float ambient : hint_range(0.0, 1.0) = 0.28;
uniform vec3 ink_color : source_color = vec3(0.02, 0.02, 0.03);

float box_mask(vec2 p, vec2 half_size, float aa) {
	vec2 d = abs(p) - half_size;
	float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
	return 1.0 - smoothstep(0.0, aa, dist);
}

void fragment() {
	vec3 n = normalize(NORMAL);
	float ndotl = clamp(dot(n, normalize(light_direction)), 0.0, 1.0);
	float shade = clamp(ambient + (1.0 - ambient) * ndotl, 0.0, 1.0);
	float luminance = dot(COLOR.rgb, vec3(0.299, 0.587, 0.114));
	float brightness = clamp(luminance * shade, 0.0, 1.0);

	// Each voxel face is one "glyph cell": UV spans 0..1 per face, so the
	// density ramp below picks a procedural glyph shape from brightness,
	// mirroring the " .:+*#@" ramp the game already uses for its 2D ascii art.
	vec2 cell = fract(UV) - 0.5;
	float aa = 0.05;
	float mask = 0.0;

	if (brightness > 0.90) {
		// '@' - solid block
		mask = box_mask(cell, vec2(0.42), aa);
	} else if (brightness > 0.72) {
		// '#' - two vertical bars + two horizontal bars
		mask = max(mask, box_mask(cell - vec2(0.16, 0.0), vec2(0.05, 0.34), aa));
		mask = max(mask, box_mask(cell + vec2(0.16, 0.0), vec2(0.05, 0.34), aa));
		mask = max(mask, box_mask(cell - vec2(0.0, 0.16), vec2(0.34, 0.05), aa));
		mask = max(mask, box_mask(cell + vec2(0.0, 0.16), vec2(0.34, 0.05), aa));
	} else if (brightness > 0.52) {
		// '*' - axis cross plus diagonal cross
		vec2 r = mat2(vec2(0.7071, 0.7071), vec2(-0.7071, 0.7071)) * cell;
		mask = max(box_mask(cell, vec2(0.32, 0.075), aa), box_mask(cell, vec2(0.075, 0.32), aa));
		mask = max(mask, max(box_mask(r, vec2(0.28, 0.06), aa), box_mask(r, vec2(0.06, 0.28), aa)));
	} else if (brightness > 0.34) {
		// '+' - axis cross
		mask = max(box_mask(cell, vec2(0.34, 0.08), aa), box_mask(cell, vec2(0.08, 0.34), aa));
	} else if (brightness > 0.16) {
		// '.' - dot
		mask = 1.0 - smoothstep(0.10, 0.10 + aa, length(cell));
	}
	// else: ' ' - empty cell, mask stays 0

	ALBEDO = mix(ink_color, COLOR.rgb * shade, mask);
}
