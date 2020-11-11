bl_info = {
    "name": "Grease Pencil export",
    "author": "Yadoob",
    "version": (1, 0),
    "blender": (2, 80, 0),
    "location": "View3D >",
    "description": "Adds a new Mesh Object",
    "warning": "",
    "doc_url": "",
    "category": "Export Grease Pencil to external Game Engine",
}


import bpy
import os
import bmesh
import mathutils


from bpy.types import Operator
from bpy.props import FloatVectorProperty
from mathutils import Vector
from pathlib import Path

class FBXExportLayout(bpy.types.Panel):

    bl_label = "Grease Pencil Exporter"
    bl_idname = "SCENE_PT_gpexportlayout"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "GP Export"
    


    def draw(self, context):
        layout = self.layout

        scene = context.scene
         
        
        row = layout.row(align=True)
        row.prop(context.scene, 'export_pathStatic', icon ="MESH_CUBE")
        row = layout.row(align=True)
        row.label(text='Unity :')
        row = layout.row(align=True)
        row.operator("object.gptounityanimated", icon ="EXPORT") #+property Separated
        row = layout.row(align=True)
        row.operator("object.gptounitystatic", icon ="EXPORT") #+property Separated
        



def exportGP (self, context, animated) :    
    
    
    gpObj = bpy.context.view_layer.objects.active
    gpLayers = gpObj.data.layers
    gpFrame = gpLayers.active.active_frame
    gpStrokes = gpFrame.strokes

    matListTemp = []
    mat3DNames = []
    matCreated = []
    matString = ""
    keyString = ""
    textureName = ""
    
    objsToExport = []
    
    path = context.scene.export_pathStatic
    
    
    ### CREATE MATERIALS ### 
    
    # get all material names in Scene  
    
    mat3D = bpy.data.materials

    for mat in mat3D :
        #print(mat, "  ",mat.is_grease_pencil)
        if not mat.is_grease_pencil :
            mat3DNames.append(mat.name + "GP") #get list of all 3D material in the scene


    # create 3D Materials from GP Materials
    for  slot in gpObj.material_slots:
        mat=slot.material
        if mat not in matListTemp :
            col = None
            
            if mat.grease_pencil.show_stroke :
                col = mat.grease_pencil.color
                 # check if there's a texture assigned
                if mat.grease_pencil.stroke_style == "TEXTURE" :
                    print(mat.grease_pencil.stroke_image.name)
                    textureName = mat.grease_pencil.stroke_image.name    
                
            if mat.grease_pencil.show_fill :
                col = mat.grease_pencil.fill_color
            
            colString = str(col[0]) + "," + str(col[1]) + "," + str(col[2]) + "," + str(col[3])
            matData = "#" + mat.name + "," + colString + "," + textureName
            matString = matString + matData
            
            matListTemp.append(mat)
            
            # create corresponding mat in Blender
            if (mat.name + "GP") not in mat3DNames :
                
                # create emissive mat with data
                mat_name = mat.name + "GP"
                matCreate = bpy.data.materials.new(mat_name)
                bpy.data.materials[mat_name].use_nodes = True

                nodes = bpy.data.materials[mat_name].node_tree.nodes

                node=nodes['Principled BSDF']
                nodes.remove(node)
                nodes.new(type='ShaderNodeEmission')
                inp = nodes['Material Output'].inputs['Surface']
                outp = nodes['Emission'].outputs['Emission']
                bpy.data.materials[mat_name].node_tree.links.new(inp,outp)
                nodes['Emission'].inputs[0].default_value = (col[0],col[1],col[2],col[3])
                            
                bpy.data.materials[mat.name].node_tree
                
                # update list with new material
                mat3DNames.append(mat.name)
                matCreated.append(matCreate)
    
    
    ### CONVERT GREASE PENCIL ###

    
    startFrame = bpy.data.scenes["Scene"].frame_start
    endFrame = bpy.data.scenes["Scene"].frame_end

    saveFrame = bpy.data.scenes["Scene"].frame_current
    
    
    order = 0
    for layer in gpLayers :
        order = order -1
        gpLayers.active = layer 
        layerName = layer.info  
        
        stringToWriteLayer = "#" + layerName + ","         
        
        frameToWrite = ""
                          
        # Check if Static or Animated               
        if (animated == True ):
        
            frameToLook = layer.frames
        else :
            framesToLook = layer.frames[0]
            
                             
        # For each Frame with keyframe                  
        for frame in layer.frames :
            
            frameNbr = frame.frame_number
            frameW = str(frameNbr) + ","
            frameToWrite = frameToWrite + frameW
                            
            bpy.data.scenes["Scene"].frame_current = frameNbr 
            bpy.context.view_layer.update()

            
        
            # check if line or Fill layer (first stroke)
            matID = frame.strokes[0].material_index
            matSlot = gpObj.material_slots[matID]
            mat = matSlot.material
            
            if mat.grease_pencil.show_stroke :
                
                # convert to curve
                bpy.ops.gpencil.convert(type='POLY', use_timing_data=True)
                
                for selObj in bpy.context.selected_objects :
                    if not selObj == gpObj :
                        #set active
                        bpy.context.view_layer.objects.active = selObj
                        #set name
                        selObj.name = layerName + "." + str(frameNbr)
                        #set bevel
                        selObj.data.extrude = 0.005
                        
                        bpy.ops.object.convert(target='MESH')
                        
                        
                        #ReOrder Layers (offset objects)   
                        vec = mathutils.Vector((0.0, (order*0.001), 0.0))
                        selObj.location = selObj.location + vec
                      
                                            
                        # Assign Materials from mat3D list
                        matName = mat.name + "GP"
                        material3D = bpy.data.materials.get(matName)
                        selObj.data.materials.append(material3D)
                        
                        selObj.select_set(False)
                        objsToExport.append(selObj)
                        
                        bpy.context.view_layer.objects.active = gpObj  
                   
                        
                       
            if mat.grease_pencil.show_fill :

                # convert to curve
                bpy.ops.gpencil.convert(type='POLY', use_timing_data=True)
                # convert to mesh
                
                # fill for every stroke
                # Assign Material
                for selObj in bpy.context.selected_objects :
                    if not selObj == gpObj :

                        #set active
                        bpy.context.view_layer.objects.active = selObj
                        #set name
                        selObj.name = layerName + "." + str(frameNbr)
                        gpObj.select_set(False)
                        
                        bpy.ops.object.convert(target='MESH')
                        
                        bpy.ops.object.mode_set(mode="EDIT")
                        
                        bm = bmesh.from_edit_mesh(selObj.data) #create face for each edge island
                        bm.edges.ensure_lookup_table()
                        loops = list()
                        edges = bm.edges
                        
                        for edge in edges:  # OPTIMSIATION
                            edge.select_set(True) # select 1st edge
                            bpy.ops.mesh.select_linked() # select all linked edges makes a full loop
                            bpy.ops.mesh.edge_face_add()
                            loops.append([f.index for f in edges if f.select])
                            bpy.ops.mesh.hide(unselected=False) # hide the detected loop
                            edges = [f for f in bm.edges if not f.hide] # update edges
                            
                        
                        bpy.ops.mesh.reveal() # unhide all face
                        bpy.ops.mesh.quads_convert_to_tris(quad_method='BEAUTY', ngon_method='BEAUTY')


                        bpy.ops.object.mode_set(mode="OBJECT")
                        
                        
                        #ReOrder Layers (offset objects)     
                        vec = mathutils.Vector((0.0, (order*0.001), 0.0))
                        selObj.location = selObj.location + vec
                                                            
                        # Assign Materials from mat3D list
                        matName = mat.name + "GP"
                        material3D = bpy.data.materials.get(matName)
                        selObj.data.materials.append(material3D)
                        
                        
                        selObj.select_set(False)
                        gpObj.select_set(True)
                        objsToExport.append(selObj)
                        
                        bpy.context.view_layer.objects.active = gpObj                      

            if (animated == True) :
                stringToWriteLayerFrame = frameToWrite[:-1]
        # write in Txt the Layer+Keyframe nbr
        if (animated == True) :
            keyString = keyString + stringToWriteLayer + stringToWriteLayerFrame

        
     
    ### EXPORT ###   
            
    #Export FBX
    gpObj.select_set(False)
    
    exportPath = bpy.path.abspath(path)
    fullPath = exportPath  + gpObj.name + ".fbx"

    
    for objToExport in objsToExport :
        objToExport.select_set(True)
        #bpy.context.view_layer.objects.active = objToExport
     
    
    
    #Recalculate Normals TODO 

   
    #run export
    bpy.ops.export_scene.fbx\
    (
    filepath=fullPath, 
    use_selection=True,  
    object_types={'EMPTY', 'MESH'}, 
    bake_space_transform=True , 
    bake_anim = False
    )

    # Export Keyframe Txt stringToWriteLayerFrame
    # Create text file with export name if it doesn' exist
   
    if (animated == True) :
        with open(exportPath + gpObj.name + "_Keys.txt", "w") as file:
            #print(keyString, "  ", file)
            file.write(keyString)
            file.close()
    # Export Materal List with properties (Color RGBA for now)  matString
    with open(exportPath + gpObj.name + "_Materials.txt", "w") as file:
        #print(matString, "  ", file)
        file.write(matString)
        file.close()


    
    ### CLEAN ###   
    
    # Clean Scene
    bpy.ops.object.delete() 
    bpy.data.scenes["Scene"].frame_current = saveFrame
    
    #Clean Materials
    for matC in matCreated :
        if not matC.is_grease_pencil :
            bpy.data.materials.remove(matC)
        

    
    


class OBJECT_OT_GpToUnityExportAnimated(Operator):
    """Export Grease Pencil Object with animation"""
    bl_idname = "object.gptounityanimated"
    bl_label = "Export Animated Grease Pencil"
    bl_options = {'REGISTER', 'UNDO'}
         
         
    def execute(self, context):

        
        #get object in selection, for each, set active and selection
        
        exportGP(self,context,True) #is animated

        return {'FINISHED'}




class OBJECT_OT_GpToUnityExportStatic(Operator):
    """Export Grease Pencil Object without animation"""
    bl_idname = "object.gptounitystatic"
    bl_label = "Export Static Grease Pencil"
    bl_options = {'REGISTER', 'UNDO'}
         
         
    def execute(self, context):
        # create 3D Materials from GP Materials
        
        exportGP(self,context,False) #is static

        return {'FINISHED'}
        
        
        
        
        
        
# Registration        

def register():
    bpy.utils.register_class(FBXExportLayout)
    bpy.utils.register_class(OBJECT_OT_GpToUnityExportAnimated)
    bpy.utils.register_class(OBJECT_OT_GpToUnityExportStatic)
    bpy.types.Scene.export_pathStatic = bpy.props.StringProperty\
    (
    name = "Folder",
    default = "",
    description = "Define the path of the project folder you want to export in",
    subtype = 'DIR_PATH'
    )


def unregister():
    bpy.utils.unregister_class(OBJECT_OT_GpToUnityExportAnimated)
    bpy.utils.unregister_class(OBJECT_OT_GpToUnityExportStatic)
    bpy.utils.unregister_class(FBXExportLayout)
    del bpy.types.Scene.export_pathStatic

if __name__ == "__main__":
    register()
