# **************************************************************************** #
#                                                                              #
#                                                         :::      ::::::::    #
#    hex.fs                                             :+:      :+:    :+:    #
#                                                     +:+ +:+         +:+      #
#    By: phtruong <marvin@42.fr>                    +#+  +:+       +#+         #
#                                                 +#+#+#+#+#+   +#+            #
#    Created: 2019/07/07 13:10:05 by phtruong          #+#    #+#              #
#    Updated: 2019/07/07 14:29:52 by phtruong         ###   ########.fr        #
#                                                                              #
# **************************************************************************** #

FeatureScript 1096;
import(path : "onshape/std/geometry.fs", version : "1096.0");

annotation { "Feature Type Name" : "Hex Nut" }
export const myFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Define the parameters of the feature type
        annotation { "Name" : "Dia" }
        isLength(definition.hexDia, { (inch) : [ 0.01, 0.4375, 10] } as LengthBoundSpec);
        annotation { "Name" : "Height" }
        isLength(definition.hexHeight, { (inch) : [ 0.01, 0.265625, 10] } as LengthBoundSpec);
        annotation { "Name" : "Inner dia" }
        isLength(definition.innerDia, { (inch) : [ 0.01, 0.25, 10] } as LengthBoundSpec);
        annotation { "Name" : "Pitch" }
        isLength(definition.pitch, { (inch) : [0.01, 0.05, 0.5] } as LengthBoundSpec);
        

    }
    {
        // Extract units from user input
        setVariable(context, "hex_dia", definition.hexDia/inch);
        setVariable(context, "hex_height", definition.hexHeight/inch);
        setVariable(context, "inner_dia", definition.innerDia/inch);
        setVariable(context, "pitch", definition.pitch/inch);
        
        //Main steps
        sketchHex(context, id);
        extrudeBase(context, id);
        sketchChamfer(context, id);
        outerChamfer(context, id);
        innerCylExtrude(context, id);
        helixGuide(context,id);
        innerSweep(context, id);
        //Clear sketches
        deleteBodies(context, id + "clear", { "entities" : qUnion([qCreatedBy(id + "hex", EntityType.BODY),
                                                                        qCreatedBy(id + "revolve_chamfer", EntityType.BODY),
                                                                        qCreatedBy(id + "sweep", EntityType.BODY)])});
    });
    /*
    ** function sketchHex:
    ** By using the law of equilateral triangle, we can find the height given one of the side (input from the user), 
    ** the distance from the center to one of the vertex is 2/3 of the height of the triangle.
    ** Thus by doing (hex_dia*sqrt(3))/2 * 2/3 or simplied to (hex_dia * sqrt(3)) / 3, 
    ** we can get the distance from the center to the vertex for the sketch.
	** Parameter:
	** 		context is Context, id is Id (passed in from main)
    ** Default unit: inch
    ** Functionality: Sketch the base for extrusion
	** Return: NULL
    */
    function sketchHex(context is Context, id is Id)
    {
        var hex_sketch = newSketch(context, id + "hex", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
		// Hex diameter from user input
        var hex_dia = getVariable(context, "hex_dia");
		// Inner hole diamter from user input
        var hole_dia = getVariable(context, "inner_dia");
        var vertex_height = (hex_dia*sqrt(3))/3;
        skRegularPolygon(hex_sketch, "nut", {
                "center" : vector(0, 0) * inch,
                "firstVertex" : vector(0, vertex_height) * inch,
                "sides" : 6
        });
        skCircle(hex_sketch, "hole", {
                "center" : vector(0, 0) * inch,
                "radius" : hole_dia/2 * inch
        });
        skSolve(hex_sketch);
    }
    /*
    ** function extrudeBase:
    ** Using the sketch previously, extrude the the hex.
	** Parameter:
	** 		context is Context, id is Id (passed in from main)
    ** Default unit: inch
    ** Functionality: Extrude the hex with the given height from user input.	
	** Return: NULL
    */
    function extrudeBase(context is Context, id is Id)
    {
		// height of the hex nut from user input
        var height = getVariable(context, "hex_height");
        opExtrude(context, id + "base_extrude", {
                "entities" : qSketchRegion(id + "hex",true),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "hex", true)}).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : height * inch
        });
    }
    /*
    ** function sketchChamfer:
    ** Sketch two right triangles to chamfer the edge of the hex.
	** Parameter:
	** 		context is Context, id is Id (passed in from main)
    ** Default unit: inch
    ** Functionality: Create sketch for revolve chamfer the hex.
	** Return: NULL
    */
    function sketchChamfer(context is Context, id is Id)
    {
        var chamfer_revolve = newSketch(context, id + "revolve_chamfer", {
                "sketchPlane" : qCreatedBy(makeId("Front"), EntityType.FACE)
        });
        var vertex_x = getVariable(context, "hex_dia")/2;
        var vertex_y = getVariable(context, "hex_height");
        var offset = vertex_x/2;
        skLineSegment(chamfer_revolve, "top_line", {
                "start" : vector(vertex_x, vertex_y) * inch,
                "end" : vector(vertex_x+offset, vertex_y) * inch
        });
        skLineSegment(chamfer_revolve, "top_right_line", {
                "start" : vector(vertex_x+offset, vertex_y) * inch,
                "end" : vector(vertex_x+offset, vertex_y-(offset/2)) * inch
        });
        skLineSegment(chamfer_revolve, "top_hypo", {
                "start" : vector(vertex_x, vertex_y) * inch,
                "end" : vector(vertex_x+offset, vertex_y-(offset/2)) * inch
        });
        skLineSegment(chamfer_revolve, "axis", {
                "start" : vector(0, 0) * inch,
                "end" : vector(0, 1) * inch,
                "construction" : true
        });
        skLineSegment(chamfer_revolve, "bottom_line", {
                "start" : vector(vertex_x, 0) * inch,
                "end" : vector(vertex_x+offset, 0) * inch
        });
        skLineSegment(chamfer_revolve, "bottom_right_line", {
                "start" : vector(vertex_x+offset, 0) * inch,
                "end" : vector(vertex_x+offset, offset/2) * inch
        });
        skLineSegment(chamfer_revolve, "bottom_hypo", {
                "start" : vector(vertex_x, 0) * inch,
                "end" : vector(vertex_x+offset, offset/2) * inch
        });
        skSolve(chamfer_revolve);
    }
    /*
    ** function outerChamfer
    ** Using the previous sketch, revolve remove the edges
	** Parameter:
	** 		context is Context, id is Id (passed in from main)
    ** Functionality: Chamfer the edge using revolve.
	** Return: NULL
    */
    function outerChamfer(context is Context, id is Id)
    {
        const axis = sketchEntityQuery(id + "revolve_chamfer", EntityType.EDGE, "axis");
        var sketch = qSketchRegion(id + "revolve_chamfer");
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
	** Parameter:
	** 		context is Context, id is Id (passed in from main)
    ** Default unit: inch
    ** Functionality: Create a guide for helix for threading
	** Return: NULL
    */
    function innerCylExtrude(context is Context, id is Id)
    {
       
        var edge = qNthElement(qCreatedBy(id + "base_extrude", EntityType.EDGE), 0);
        var helix_height = getVariable(context, "hex_height")+0.15;
        opExtrude(context, id + "inner_cyl", {
                "entities" : edge,
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "hex")}).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : helix_height * inch
        });
    }
    /*
    ** function helixGuide
    ** Using the previous extrude, create a helix for threading
	** Parameter:
	** 		context is Context, id is Id (passed in from main)
    ** Default unit: inch
    ** Functionality: Takes in user input of pitch size and create a helix
	** Return: NULL
    */
    function helixGuide(context is Context, id is Id)
    {
        var pitch = getVariable(context, "pitch");
        helix(context, id + "thread_helix", {
                "helixType" : HelixType.PITCH,
                "entities" : qCreatedBy(id + "inner_cyl", EntityType.FACE),
                "helicalPitch" : pitch * inch,
                "startangle" : 0 * degree,
                "handedness" :  Direction.CW,
                "startAngle" : 360 * degree
        });
    }
    /*
    ** function innerSweep
    ** Using the previous helixGuide, create and equilateral triangle to sweep remove the inner cylinder
	** Parameter:
	** 		context is Context, id is Id (passed in from main)
    ** Default unit: inch
    ** Functionality: Remove sweep the inner hex nut
	** Return: NULL
    */
    function innerSweep(context is Context, id is Id)
    {
        var sweep_sketch = newSketch(context, id + "sweep", {
                "sketchPlane" : qCreatedBy(makeId("Front"), EntityType.FACE)
        });
        var vertex_x = getVariable(context, "inner_dia")/2;
        var vertex_y = getVariable(context, "pitch")-0.01;
        skLineSegment(sweep_sketch, "line1", {
                "start" : vector(vertex_x, 0) * inch,
                "end" : vector(vertex_x, -vertex_y) * inch
        });
        skLineSegment(sweep_sketch, "line2", {
                "start" : vector(vertex_x, 0) * inch,
                "end" : vector(vertex_x+((vertex_y*sqrt(3))/2), -vertex_y/2) * inch
        });
        skLineSegment(sweep_sketch, "line3", {
                "start" : vector(vertex_x, -vertex_y) * inch,
                "end" : vector(vertex_x+((vertex_y*sqrt(3))/2), -vertex_y/2) * inch
        });
        skSolve(sweep_sketch);
        sweep(context, id + "inner_sweep", {
                "operationType" : NewBodyOperationType.REMOVE,
                "profiles" : qSketchRegion(id + "sweep"),
                "path" : qCreatedBy(id + "thread_helix", EntityType.EDGE)
        });
    }

