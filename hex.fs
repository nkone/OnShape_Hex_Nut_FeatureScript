# **************************************************************************** #
#                                                                              #
#                                                         :::      ::::::::    #
#    hex.fs                                             :+:      :+:    :+:    #
#                                                     +:+ +:+         +:+      #
#    By: phtruong <marvin@42.fr>                    +#+  +:+       +#+         #
#                                                 +#+#+#+#+#+   +#+            #
#    Created: 2019/07/07 17:56:14 by phtruong          #+#    #+#              #
#    Updated: 2019/07/07 17:56:52 by phtruong         ###   ########.fr        #
#                                                                              #
# **************************************************************************** #

FeatureScript 1096;
import(path : "onshape/std/geometry.fs", version : "1096.0");

annotation { "Feature Type Name" : "Hex Nut" }
export const myFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Define the parameters of the feature type
        annotation { "Name" : "Hex Diameter" }
        isLength(definition.hexDia, { (inch) : [ 0.01, 0.4375, 10] } as LengthBoundSpec);
        annotation { "Name" : "Height" }
        isLength(definition.hexHeight, { (inch) : [ 0.01, 0.265625, 10] } as LengthBoundSpec);
        annotation { "Name" : "Hole diameter" }
        isLength(definition.holeDia, { (inch) : [ 0.01, 0.25, 10] } as LengthBoundSpec);
        annotation { "Name" : "Pitch" }
        isLength(definition.pitch, { (inch) : [0.01, 0.05, 0.5] } as LengthBoundSpec);
        

    }
    {
        // Extract units from user input
        var hexDia = definition.hexDia/inch;
        var hexHeight = definition.hexHeight/inch;
        var holeDia = definition.holeDia/inch;
        var pitch = definition.pitch/inch;
        
        //Main steps
        sketchHex(context, id, hexDia, holeDia);
        extrudeBase(context, id, hexHeight);
        sketchChamfer(context, id, hexDia, hexHeight);
        outerChamfer(context, id);
        innerCylExtrude(context, id, hexHeight);
        helixGuide(context, id, pitch);
        innerSweep(context, id, pitch, holeDia);
        //Clear sketches
        deleteBodies(context, id + "clear", { "entities" : qUnion([qCreatedBy(id + "hex", EntityType.BODY),
                                                                        qCreatedBy(id + "revolveChamfer", EntityType.BODY),
                                                                        qCreatedBy(id + "sweep", EntityType.BODY)])});
    });
    /*
    ** function sketchHex:
    ** Parameters:
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data struture)
    ** [hexDia] is hex diameter
    ** [holeDia] is the inner diameter of the hex
    ** Default unit: inch
    ** Functionality: Sketch the base for extrusion
    ** Return: NULL
    */
    function sketchHex(context is Context, id is Id, hexDia is number, holeDia is number)
    {
        var hexSketch = newSketch(context, id + "hex", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
        var vertexHeight = (hexDia*sqrt(3))/3;
        skRegularPolygon(hexSketch, "nut", {
                "center" : vector(0, 0) * inch,
                "firstVertex" : vector(0, vertexHeight) * inch,
                "sides" : 6
        });
        skCircle(hexSketch, "hole", {
                "center" : vector(0, 0) * inch,
                "radius" : holeDia/2 * inch
        });
        skSolve(hexSketch);
    }
    /*
    ** function extrudeBase:
    ** Using the sketch previously, extrude the the hex.
    ** Parameters:
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data struture)
    ** [hexHeight] is height of the nut
    ** Default unit: inch
    ** Functionality: Extrude the hex with the given height from user input.
    ** Return: NULL
    */
    function extrudeBase(context is Context, id is Id, hexHeight is number)
    {
        opExtrude(context, id + "baseExtrude", {
                "entities" : qSketchRegion(id + "hex",true),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "hex", true)}).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : hexHeight * inch
        });
    }
    /*
    ** function sketchChamfer:
    ** Sketch two right triangles to chamfer the edge of the hex.
    ** Paramters:
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data struture)
    ** [hexDia] is hex diameter
    ** [hexHeight] is height of the nut
    ** Default unit: inch
    ** Functionality: Create sketch for revolve chamfer the hex.
	** Return: NULL
    */
    function sketchChamfer(context is Context, id is Id, hexDia is number, hexHeight is number)
    {
        var chamferRevolve = newSketch(context, id + "revolveChamfer", {
                "sketchPlane" : qCreatedBy(makeId("Front"), EntityType.FACE)
        });
        var vertexX = hexDia/2;
        var vertexY = hexHeight;
        var offset = vertexX/2;
        skLineSegment(chamferRevolve, "topLine", {
                "start" : vector(vertexX, vertexY) * inch,
                "end" : vector(vertexX+offset, vertexY) * inch
        });
        skLineSegment(chamferRevolve, "topRightLine", {
                "start" : vector(vertexX+offset, vertexY) * inch,
                "end" : vector(vertexX+offset, vertexY-(offset/2)) * inch
        });
        skLineSegment(chamferRevolve, "topHypo", {
                "start" : vector(vertexX, vertexY) * inch,
                "end" : vector(vertexX+offset, vertexY-(offset/2)) * inch
        });
        skLineSegment(chamferRevolve, "axis", {
                "start" : vector(0, 0) * inch,
                "end" : vector(0, 1) * inch,
                "construction" : true
        });
        skLineSegment(chamferRevolve, "bottomLine", {
                "start" : vector(vertexX, 0) * inch,
                "end" : vector(vertexX+offset, 0) * inch
        });
        skLineSegment(chamferRevolve, "bottomRightLine", {
                "start" : vector(vertexX+offset, 0) * inch,
                "end" : vector(vertexX+offset, offset/2) * inch
        });
        skLineSegment(chamferRevolve, "bottomHypo", {
                "start" : vector(vertexX, 0) * inch,
                "end" : vector(vertexX+offset, offset/2) * inch
        });
        skSolve(chamferRevolve);
    }
    /*
    ** function outerChamfer
    ** Paramters:
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data struture)
    ** Using the previous sketch, revolve remove the edges
    ** Functionality: Chamfer the edge using revolve.
    */
    function outerChamfer(context is Context, id is Id)
    {
        const axis = sketchEntityQuery(id + "revolveChamfer", EntityType.EDGE, "axis");
        var sketch = qSketchRegion(id + "revolveChamfer");
        revolve(context, id + "chamfer", {
            "operationType" : NewBodyOperationType.REMOVE,
            "entities" : sketch,
            "axis" : axis,
            "revolveType" : RevolveType.FULL
        });
    }
    /*
    ** function innerCylExtrude
    ** Uses simple extrude on the surface on the inner hex to create a guide for next helix operation
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data struture)
    ** [hexHeight] is height of the nut
    ** Default unit: inch
    ** Functionality: Create a guide for helix for threading
    ** Return: NULL
    */
    function innerCylExtrude(context is Context, id is Id, hexHeight is number)
    {
       
        var edge = qNthElement(qCreatedBy(id + "baseExtrude", EntityType.EDGE), 0);
        var helixHeight = hexHeight+0.15;
        opExtrude(context, id + "innerCyl", {
                "entities" : edge,
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "hex")}).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : helixHeight * inch
        });
    }
    /*
    ** function helixGuide
    ** Using the previous extrude, create a helix for threading
    ** Paramters:
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data struture)
    ** [pitch] is thread pitch size
    ** Default unit: inch
    ** Functionality: Takes in user input of pitch size and create a helix
    ** Return: NULL
    */
    function helixGuide(context is Context, id is Id, pitch is number)
    {
        helix(context, id + "threadHelix", {
                "helixType" : HelixType.PITCH,
                "entities" : qCreatedBy(id + "innerCyl", EntityType.FACE),
                "helicalPitch" : pitch * inch,
                "startangle" : 0 * degree,
                "handedness" :  Direction.CW,
                "startAngle" : 360 * degree
        });
    }
    /*
    ** function innerSweep
    ** Using the previous helixGuide, create and equilateral triangle to sweep remove the inner cylinder
    ** Parameters:
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data struture)
    ** [pitch] is thread pitch size
    ** [holeDia] is inner diameter of the nut
    ** Default unit: inch
    ** Functionality: Remove sweep the inner hex nut.
	** Return: NULL
    */
    function innerSweep(context is Context, id is Id, pitch is number, holeDia is number)
    {
        var sweepSketch = newSketch(context, id + "sweep", {
                "sketchPlane" : qCreatedBy(makeId("Front"), EntityType.FACE)
        });
        var vertexX = holeDia/2;
        var vertexY = pitch-0.01;
        skLineSegment(sweepSketch, "line1", {
                "start" : vector(vertexX, 0) * inch,
                "end" : vector(vertexX, -vertexY) * inch
        });
        skLineSegment(sweepSketch, "line2", {
                "start" : vector(vertexX, 0) * inch,
                "end" : vector(vertexX+((vertexY*sqrt(3))/2), -vertexY/2) * inch
        });
        skLineSegment(sweepSketch, "line3", {
                "start" : vector(vertexX, -vertexY) * inch,
                "end" : vector(vertexX+((vertexY*sqrt(3))/2), -vertexY/2) * inch
        });
        skSolve(sweepSketch);
        sweep(context, id + "innerSweep", {
                "operationType" : NewBodyOperationType.REMOVE,
                "profiles" : qSketchRegion(id + "sweep"),
                "path" : qCreatedBy(id + "threadHelix", EntityType.EDGE)
        });
    } 
