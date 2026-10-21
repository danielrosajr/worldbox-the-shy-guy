#!/usr/bin/env python3
"""SCP-096 procedural GLB v0.2.15.
Original low-poly anatomical sculpt for WorldFall; no copyrighted mesh used.
Requires numpy and trimesh on the development machine, not in the game.
Coordinates follow the verified 0.2.14 GLB: +Z up, face toward -Y.
"""
from pathlib import Path
import math
import numpy as np
import trimesh

RNG = np.random.default_rng(96)
SCENE = trimesh.Scene()
COUNT = 0
PALE = np.array([211., 214., 207.])
BONE = np.array([224., 222., 207.])
SHADOW = np.array([126., 139., 139.])
SICK = np.array([173., 177., 171.])
BLACK = np.array([13., 16., 22.])
DARK = np.array([32., 31., 40.])
TEAR = np.array([48., 61., 67.])
TOOTH = np.array([219., 213., 191.])

def colors(vertices, base=PALE, variation=8., lower=False):
    n=len(vertices)
    rnd=RNG.normal(0,variation,(n,1))
    vertical=(vertices[:,2:3]-1.0)*2.4
    side=(vertices[:,1:2]+.15)*7
    col=np.asarray(base)[None,:]+rnd+vertical+side
    if lower: col-=np.maximum(0,1.2-vertices[:,2:3])*7
    return np.column_stack((np.clip(col,0,255).astype(np.uint8),
                            np.full(n,255,dtype=np.uint8)))
def style(mesh, col=PALE, variation=8., lower=False):
    mesh.visual.vertex_colors=colors(mesh.vertices,col,variation,lower)
    return mesh
def orb(c, s, base=PALE, variation=8., subs=2, irregular=0., seed=0):
    m=trimesh.creation.icosphere(subdivisions=subs,radius=1.)
    v=m.vertices.copy()
    if irregular:
        r=np.random.default_rng(seed+96)
        v=v*(1+r.normal(0,irregular,(len(v),1)))
    m.vertices=v*np.array(s)[None,:]+np.array(c)[None,:]
    return style(m,base,variation)
def sweep(points, radii, col=PALE, variation=7., sides=11,
          irregular=.035, seed=0):
    """Smooth tapered organic tube with ring structure."""
    points=np.asarray(points,dtype=float)
    radii=np.asarray(radii,dtype=float)
    if radii.ndim==1: radii=np.column_stack([radii,radii])
    verts=[]
    seeded=np.random.default_rng(seed+96)
    for i,p in enumerate(points):
        tangent=points[min(i+1,len(points)-1)]-points[max(i-1,0)]
        tangent=tangent/(np.linalg.norm(tangent)+1e-12)
        ref=np.array([0.,1.,0.]) if abs(tangent[1])<.8 else np.array([1.,0.,0.])
        u=np.cross(tangent,ref); u=u/(np.linalg.norm(u)+1e-12)
        v=np.cross(tangent,u); v=v/(np.linalg.norm(v)+1e-12)
        for j in range(sides):
            a=j*2*math.pi/sides
            wiggle=1.0+seeded.normal(0,irregular)
            verts.append(p+wiggle*(u*math.cos(a)*radii[i,0]+
                                   v*math.sin(a)*radii[i,1]))
    faces=[]
    for i in range(len(points)-1):
        for j in range(sides):
            k=i*sides+j
            n=i*sides+(j+1)%sides
            faces.extend([[k,n,n+sides],[k,n+sides,k+sides]])
    for j in range(1,sides-1):
        faces.append([0,j+1,j])
        start=(len(points)-1)*sides
        faces.append([start,start+j,start+j+1])
    m=trimesh.Trimesh(vertices=np.asarray(verts),faces=np.asarray(faces),process=False)
    return style(m,col,variation)
def add(name,meshes):
    global COUNT
    if not isinstance(meshes,list): meshes=[meshes]
    m=trimesh.util.concatenate(meshes)
    SCENE.add_geometry(m,geom_name=name,node_name=name)
    COUNT+=len(m.faces)

# Torso. Emaciated, deeply hunched neck and hollow abdomen.
torso=[
 [0,.037,1.19],[0,.012,1.28],[0,-.018,1.42],
 [0,.015,1.53],[0,.048,1.65],[0,.059,1.74],[0,.040,1.81]]
torso_rad=[[.100,.075],[.087,.065],[.108,.080],
           [.149,.100],[.205,.127],[.229,.113],[.112,.080]]
trunk=sweep(torso,torso_rad,variation=12,sides=17,irregular=.07)
body=[trunk]
# Breastbone, collarbones and shoulder caps.
body.append(sweep([[0,-.139,1.47],[0,-.160,1.56],[0,-.132,1.68]],
                  [.021,.024,.018],BONE,5,sides=9))
for side in [-1,1]:
    body.append(orb([side*.229,.018,1.765],[.073,.065,.065],
                    PALE,10,subs=2,seed=side+5))
    body.append(sweep([[0,-.105,1.748],[side*.10,-.13,1.77],
                       [side*.215,-.071,1.761]], [.018,.022,.026],
                       BONE,5,sides=8))
    # Rib arcs over the sternum and the chest sides.
    for i in range(4):
        z=1.682-i*.073
        spread=[.165,.19,.16,.12][i]
        pts=[[side*.015,-.156,z],[side*spread*.46,-.173,z+.018],
             [side*spread*.85,-.119,z+.016],
             [side*spread,-.044,z+.002]]
        body.append(sweep(pts,[.009,.014,.016,.010],BONE,6,sides=7,seed=i+side*5))
    # Severe hip joints and pointed iliac crests.
    body.append(orb([side*.102,.033,1.20],[.079,.070,.092],PALE,9,seed=side+17))
    body.append(sweep([[side*.031,-.042,1.234],[side*.108,-.070,1.239],
                       [side*.158,-.015,1.272]], [.011,.025,.009],
                       BONE,6,sides=8))
# Distinct spinal ridge along a curved rear plane.
body.append(sweep([[0,.10,1.24],[0,.11,1.38],[0,.137,1.53],
                   [0,.150,1.68],[0,.115,1.78]],
                  [.018,.021,.019,.025,.016],SICK,5,sides=9))
add("body",body)

# Head: elongated, slumped forward skull, sharp cheeks, protruding jaw.
head_parts=[
    orb([0,-.128,1.970],[.165,.143,.241],PALE,10,subs=3,irregular=.018,seed=1),
    orb([0,-.163,1.806],[.099,.092,.083],PALE,12,subs=2),
    orb([0,-.090,1.760],[.071,.072,.080],PALE,7,subs=2),
    sweep([[0,-.091,1.779],[0,-.100,1.840],[0,-.067,1.870]],
          [.060,.064,.068],PALE,8,sides=13)
]
for side in [-1,1]:
    # Sunken cheekbones protrude around deeply recessed face.
    head_parts.append(orb([side*.113,-.215,1.935],
                          [.060,.053,.049],BONE,9,subs=2,seed=side+32))
    head_parts.append(sweep([[side*.102,-.223,1.932],
                             [side*.120,-.205,1.867],
                             [side*.059,-.214,1.810]], [.016,.021,.023],
                             SICK,8,sides=8))
    head_parts.append(sweep([[side*.042,-.274,2.078],
                             [side*.10,-.286,2.098],
                             [side*.141,-.248,2.077]], [.013,.015,.011],
                             BONE,6,sides=7))
# Bony nose and asymmetric nostrils.
head_parts.append(sweep([[0,-.257,2.040],[0,-.293,1.988],
                         [0,-.315,1.947]], [.021,.022,.027],BONE,6,sides=10))
head_parts.append(orb([0,-.313,1.947],[.032,.027,.019],SICK,8,subs=1))
# Slender neck tendons.
head_parts.append(sweep([[0,-.066,1.805],[.018,-.091,1.779],
                         [0,-.062,1.725]], [.055,.042,.043],PALE,8,sides=11))
add("head",head_parts)

# Distinct dark hollow eye sockets: mesh visibly faces negative Y.
eyes=[]
for side in [-1,1]:
    c=[side*.077,-.268,2.051]
    eyes.append(orb(c,[.054,.023,.072],BLACK,4,subs=2,seed=side+50))
    eyes.append(orb([side*.077,-.289,2.045],[.030,.009,.044],
                    DARK,3,subs=1))
    # Minimal sickly eye glint, rather than round cartoon eyeballs.
    eyes.append(orb([side*.094,-.299,2.068],[.007,.004,.010],
                    [83,83,78],3,subs=1))
    # Four narrow tear trails down hollow cheek.
    eyes.append(sweep([[side*.092,-.293,2.009],
                       [side*.112,-.274,1.971],
                       [side*.120,-.256,1.913],
                       [side*.107,-.248,1.883]],
                      [.005,.007,.006,.002],TEAR,4,sides=7))
    eyes.append(sweep([[side*.059,-.296,2.005],
                       [side*.055,-.278,1.966],
                       [side*.066,-.253,1.934]],
                      [.003,.005,.002],TEAR,4,sides=6))
add("facial_eyes_and_tears",eyes)

# Facial mouth: long black vertical sob/scream surrounded by thin lips,
# discernible even at reduced WorldFall 3D scale.
mouth=[
    orb([0,-.275,1.860],[.076,.018,.118],DARK,3,subs=3),
    orb([0,-.291,1.868],[.058,.011,.095],BLACK,2,subs=2),
]
for side in [-1,1]:
    mouth.append(sweep([[side*.041,-.293,1.960],
                        [side*.074,-.289,1.913],
                        [side*.077,-.286,1.847],
                        [side*.053,-.276,1.775]],
                       [.007,.009,.011,.008],SICK,5,sides=8))
# Upper/lower teeth are discrete, small dirty bone-like shapes.
for i in [-2,-1,0,1,2]:
    x=i*.020
    width=max(.0055,.010-abs(i)*.001)
    mouth.append(sweep([[x,-.310,1.932],[x*.96,-.309,1.908]],
                       [width,.0035],TOOTH,4,sides=6))
    mouth.append(sweep([[x,-.304,1.806],[x*.96,-.308,1.828]],
                       [width,.0035],TOOTH,4,sides=6))
add("facial_mouth_and_teeth",mouth)

# Long, jointed arms and palms with five individual bent fingers.
for side, key in [(-1,"l"),(1,"r")]:
    arm=[]
    arm.append(sweep([[side*.236,.008,1.751],
                      [side*.302,.015,1.576],
                      [side*.347,-.001,1.337],
                      [side*.370,-.033,1.185]],
                     [[.070,.072],[.057,.055],[.044,.045],[.052,.050]],
                     PALE,11,sides=13,irregular=.05))
    arm.append(orb([side*.371,-.035,1.183],
                   [.047,.045,.049],BONE,8,subs=2))
    arm.append(sweep([[side*.373,-.034,1.183],
                      [side*.395,-.062,1.045],
                      [side*.418,-.078,.870],
                      [side*.443,-.100,.730]],
                     [[.049,.054],[.050,.048],[.035,.038],[.030,.030]],
                     PALE,11,sides=13,irregular=.045))
    arm.append(orb([side*.447,-.102,.713],
                   [.037,.035,.044],BONE,9,subs=2))
    palm=sweep([[side*.443,-.100,.722],[side*.453,-.116,.639],
                [side*.457,-.132,.550]],
               [[.050,.034],[.059,.036],[.054,.030]],
               PALE,9,sides=12)
    arm.append(palm)
    for j in range(5):
        # Index & middle fingers disproportionately long, not five spikes.
        reach=[.24,.30,.33,.29,.20][j]
        lateral=(j-2)*.026
        x=side*(.454 + lateral*.74)
        arm.append(sweep([[x,-.135,.565],
                          [x+side*lateral*.15,-.155,.545-reach*.38],
                          [x+side*lateral*.47,-.162,.545-reach*.72],
                          [x+side*lateral*.53,-.177,.535-reach]],
                         [[.015,.012],[.012,.012],[.010,.010],[.0035,.004]],
                         PALE,9,sides=8,irregular=.02,seed=j*10+side))
        if j in [1,2,3]:
            arm.append(orb([x+side*lateral*.47,-.163,.545-reach*.72],
                           [.014,.015,.017],BONE,5,subs=1))
    # Dark knuckle creases.
    for j in [-1,0,1]:
        arm.append(sweep([[side*(.45+j*.022),-.164,.588],
                          [side*(.45+j*.022),-.160,.568]],
                         [.004,.004],SHADOW,4,sides=6))
    add("claw_"+key,arm)

# Long shins, subtle kneecaps, splayed pale feet.
for side,key in [(-1,"l"),(1,"r")]:
    leg=[]
    leg.append(sweep([[side*.117,.027,1.194],
                      [side*.140,-.028,1.039],
                      [side*.168,-.053,.795],
                      [side*.170,-.043,.670]],
                     [[.077,.070],[.073,.064],[.054,.052],[.056,.050]],
                     PALE,10,sides=14))
    leg.append(orb([side*.174,-.075,.669],[.047,.044,.058],BONE,7))
    leg.append(sweep([[side*.169,-.043,.670],
                      [side*.149,.011,.495],
                      [side*.127,.037,.285],
                      [side*.132,.009,.144]],
                     [[.049,.052],[.045,.040],[.030,.035],[.040,.034]],
                     PALE,9,sides=13))
    leg.append(orb([side*.136,-.004,.145],[.038,.034,.045],SICK,7))
    leg.append(orb([side*.151,-.090,.077],[.068,.146,.046],PALE,9,subs=2))
    for j in range(4):
        spread=(j-1.5)*.032
        xx=side*.148+spread
        leg.append(sweep([[xx,-.139,.087],[xx+spread*.12,-.204,.062],
                          [xx+spread*.24,-.265+(j%2)*.013,.053]],
                         [.020,.014,.004],PALE,6,sides=8))
    add("leg_"+key,leg)

# A few unobtrusive cold patches on joints.
for i,(x,y,z,s) in enumerate([
    (0,-.178,1.405,[.075,.006,.032]),
    (-.183,-.090,.684,[.039,.008,.023]),
    (.183,-.090,.684,[.039,.008,.023]),
]):
    add("skin_shadow_%d"%i, orb([x,y,z],s,[150,155,156],5,subs=1))

out=Path(__file__).resolve().parents[1]/"Worldfall/models/animal_scp096_shyguy.glb"
out.parent.mkdir(parents=True,exist_ok=True)
data=SCENE.export(file_type="glb")
out.write_bytes(data)
print(f"Generated {out.name}: {len(data)} bytes, {COUNT} triangles, {len(SCENE.geometry)} meshes")
print("Bounds:",np.round(SCENE.bounds,3))
