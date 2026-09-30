function slug(text) {
  return text
    .toLowerCase()
    .replace(/['’]/g, '')
    .replace(/[^\p{L}\p{N}\s_-]/gu, ' ')
    .trim()
    .replace(/\s+/g, '-');
}

export function slugger(reserved = []) {
  const seen = new Map(reserved.map(id => [id, 1]));
  return text => {
    const base = slug(text) || 'section';
    const count = seen.get(base) ?? 0;
    seen.set(base, count + 1);
    return count ? `${base}-${count + 1}` : base;
  };
}
