"""Remove the display stand Meshy's image-to-3D left fused under the zombie's feet, and drop the vertices only it used.
Usage: python -I glbcut.py in.glb out.glb [--dry]"""
import json, struct, sys
import numpy as np

src, dst = sys.argv[1], sys.argv[2]
dry = '--dry' in sys.argv
data = open(src, 'rb').read()
jlen = struct.unpack('<I', data[12:16])[0]
j = json.loads(data[20:20 + jlen])
bin_start = 20 + jlen + 8
binc = data[bin_start:bin_start + struct.unpack('<I', data[20 + jlen:24 + jlen])[0]]
DT = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}
NC = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}

def read(i):
    a = j['accessors'][i]; bv = j['bufferViews'][a['bufferView']]
    n = NC[a['type']]; dt = DT[a['componentType']]
    if bv.get('byteStride') not in (None, n * np.dtype(dt).itemsize): raise SystemExit('interleaved data not handled')
    off = bv.get('byteOffset', 0) + a.get('byteOffset', 0)
    return np.frombuffer(binc, dt, a['count'] * n, off).reshape(a['count'], n).copy()

prim = j['meshes'][0]['primitives'][0]
pos = read(prim['attributes']['POSITION'])
tri = read(prim['indices']).reshape(-1, 3)
far = (np.abs(pos[:, 0]) > .4) | (np.abs(pos[:, 2]) > .4)
top = float(pos[far, 1].max())
cut = top + .004
low = pos[tri][:, :, 1] < cut                      # per corner
drop = low.all(1)
keep = tri[~drop]
used = np.unique(keep)
print(f'stand top {top:.4f} m; triangles {len(tri)} -> {len(keep)}; vertices {len(pos)} -> {len(used)}')
kp = pos[used]
print('kept min', kp.min(0).round(4), 'max', kp.max(0).round(4))
near = kp[:, 1] < cut + .02
print('kept vertices within 2 cm above the cut:', int(near.sum()), 'their |x| max', np.abs(kp[near, 0]).max().round(3) if near.any() else None,
      '|z| max', np.abs(kp[near, 2]).max().round(3) if near.any() else None)
if dry: sys.exit()

remap = np.full(len(pos), -1, np.int64); remap[used] = np.arange(len(used))
new_idx = remap[keep].astype(np.uint32).reshape(-1)

# Rebuild the binary chunk: every bufferView copied as is, except the primitive's attributes and indices, rewritten.
replace = {}
for name, ai in prim['attributes'].items():
    replace[ai] = read(ai)[used]
replace[prim['indices']] = new_idx.reshape(-1, 1)
for ai, arr in replace.items():
    a = j['accessors'][ai]
    a['count'] = int(arr.shape[0]) if ai != prim['indices'] else int(new_idx.size)
    a.pop('byteOffset', None)
    if 'min' in a or 'max' in a:
        a['min'] = arr.min(0).tolist(); a['max'] = arr.max(0).tolist()
    if ai == prim['indices']:
        a['componentType'] = 5125
        a['min'] = [int(new_idx.min())]; a['max'] = [int(new_idx.max())]
view_of = {j['accessors'][ai]['bufferView']: ai for ai in replace}
users = {}
for ai, a in enumerate(j['accessors']):
    if 'bufferView' in a: users.setdefault(a['bufferView'], []).append(ai)
for v, acs in users.items():
    if v in view_of and len(acs) != 1: raise SystemExit(f'bufferView {v} is shared; not handled')
out = bytearray()
for v, bv in enumerate(j['bufferViews']):
    if v in view_of:
        arr = replace[view_of[v]]
        blob = np.ascontiguousarray(arr.astype(DT[j['accessors'][view_of[v]]['componentType']])).tobytes()
    else:
        o = bv.get('byteOffset', 0); blob = binc[o:o + bv['byteLength']]
    while len(out) % 4: out.append(0)
    bv['byteOffset'] = len(out); bv['byteLength'] = len(blob); bv.pop('byteStride', None)
    out += blob
while len(out) % 4: out.append(0)
j['buffers'] = [{'byteLength': len(out)}]
j.setdefault('asset', {})['extras'] = {'pz_note': 'Meshy display stand removed (triangles wholly below its top, and the vertices only they used).'}
js = json.dumps(j, separators=(',', ':')).encode()
while len(js) % 4: js += b' '
glb = struct.pack('<4sII', b'glTF', 2, 12 + 8 + len(js) + 8 + len(out)) + struct.pack('<I4s', len(js), b'JSON') + js + struct.pack('<I4s', len(out), b'BIN\x00') + bytes(out)
open(dst, 'wb').write(glb)
print('wrote', dst, len(glb), 'bytes')
