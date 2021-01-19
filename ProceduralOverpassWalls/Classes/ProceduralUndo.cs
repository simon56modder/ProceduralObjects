using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Localization;

namespace ProceduralObjects.Classes
{
    public class EditingStep
    {
        public EditingStep() { }

        public EditingStep(Vertex[] prevTempBuffer, Vertex[] nextTempBuffer)
        {
            type = StepType.vertices;
            verticesMoved = new Dictionary<Vertex, Vertex>();
            for (int i = 0; i < prevTempBuffer.Length; i++)
            {
                if (nextTempBuffer[i].Position != prevTempBuffer[i].Position)
                    this.verticesMoved.Add(new Vertex(prevTempBuffer[i]), new Vertex(nextTempBuffer[i]));
            }
        }
        public EditingStep(Vector3 prevPos, Vector3 nextPos)
        {
            type = StepType.position;
            positions = new KeyValuePair<Vector3, Vector3>(prevPos, nextPos);
        }
        public EditingStep(Quaternion prevRot, Quaternion nextRot)
        {
            type = StepType.rotation;
            rotations = new KeyValuePair<Quaternion, Quaternion>(prevRot, nextRot);
        }
        public EditingStep(Vector3 prevPos, Vector3 nextPos, Quaternion prevRot, Quaternion nextRot)
        {
            type = StepType.moveTo;
            positions = new KeyValuePair<Vector3, Vector3>(prevPos, nextPos);
            rotations = new KeyValuePair<Quaternion, Quaternion>(prevRot, nextRot);
        }
        public EditingStep(StepType type) // for mirror
        {
            this.type = type;
        }
        public EditingStep(StepType type, float factor) // for stretch
        {
            this.type = type;
            this.stretchFactor = factor;
        }

        public Dictionary<Vertex, Vertex> verticesMoved;
        public StepType type;
        public AxisEditionState axisUsed;
        public KeyValuePair<Vector3, Vector3> positions;
        public KeyValuePair<Quaternion, Quaternion> rotations;
        public float stretchFactor;

        public void UndoPosRotMoveTo(ProceduralObject obj)
        {
            if (type == StepType.position || type == StepType.moveTo)
            {
                obj.m_position = positions.Key;
            }
            if (type == StepType.rotation || type == StepType.moveTo)
            {
                obj.m_rotation = rotations.Key;
            }
        }
        public void RedoPosRotMoveTo(ProceduralObject obj)
        {
            if (type == StepType.position || type == StepType.moveTo)
            {
                obj.m_position = positions.Value;
            }
            if (type == StepType.rotation || type == StepType.moveTo)
            {
                obj.m_rotation = rotations.Value;
            }
        }

        public Vertex[] UndoVerticesStep(Vertex[] currentTempBuffer)
        {
            for (int i = 0; i < verticesMoved.Count; i++)
            {
                currentTempBuffer[verticesMoved.Keys.ToList()[i].Index].Position = verticesMoved.Keys.ToList()[i].Position;
            }
            return currentTempBuffer;
        }
        public Vertex[] RedoVerticesStep(Vertex[] currentTempBuffer)
        {
            for (int i = 0; i < verticesMoved.Count; i++)
            {
                currentTempBuffer[verticesMoved.Values.ToList()[i].Index].Position = verticesMoved.Values.ToList()[i].Position;
            }
            return currentTempBuffer;
        }

        public string GetLocalizedStepString()
        {
            switch (type)
            {
                case StepType.vertices:
                    return string.Format(LocalizationManager.instance.current["history_vertices"], verticesMoved.Count);
                case StepType.position:
                    return LocalizationManager.instance.current["history_position"];
                case StepType.rotation:
                    return LocalizationManager.instance.current["history_rotation"];
                case StepType.moveTo:
                    return LocalizationManager.instance.current["history_moveTo"];
                case StepType.mirrorX:
                    return string.Format(LocalizationManager.instance.current["history_mirror"], "X");
                case StepType.mirrorY:
                    return string.Format(LocalizationManager.instance.current["history_mirror"], "Y");
                case StepType.mirrorZ:
                    return string.Format(LocalizationManager.instance.current["history_mirror"], "Z");
                case StepType.stretchX:
                case StepType.stretchY:
                case StepType.stretchZ:
                    return string.Format(LocalizationManager.instance.current["history_scale"], stretchFactor);
            }
            return string.Empty;
        }

        public enum StepType
        {
            vertices,
            position,
            rotation,
            moveTo,
            mirrorX,
            mirrorY,
            mirrorZ,
            stretchX,
            stretchY,
            stretchZ,
            none
        }
    }
    public class HistoryBuffer
    {
        public HistoryBuffer(ProceduralObject obj)
        {
            stepsDone = new List<EditingStep>();
            stepsUndone = new List<EditingStep>();
            this.obj = obj;
            prevTempBuffer = null;
            prevTempPos = Vector3.zero;
            prevTempRot = Quaternion.identity;
            currentStepType = EditingStep.StepType.none;
            axisUsed = AxisEditionState.none;
        }

        public List<EditingStep> stepsDone;
        public List<EditingStep> stepsUndone;
        public ProceduralObject obj;

        private Vertex[] prevTempBuffer;
        private EditingStep.StepType currentStepType;
        public AxisEditionState axisUsed;
        public Vector3 prevTempPos;
        private Quaternion prevTempRot;
        public float currentStretchFactor;

        public bool CanUndo
        {
            get
            {
                if (stepsDone == null || currentStepType != EditingStep.StepType.none)
                    return false;
                return stepsDone.Count > 0;
            }
        }
        public bool CanRedo
        {
            get
            {
                if (stepsUndone == null || currentStepType != EditingStep.StepType.none)
                    return false;
                return stepsUndone.Count > 0;
            }
        }
        public void InitializeNewStep(EditingStep.StepType type, Vertex[] tempbuffer)
        {
            if (currentStepType != EditingStep.StepType.none) // if already registering an action then don't overlap the other one, just make it one action.
                return;
            switch (type)
            {
                case EditingStep.StepType.vertices:
                    if (tempbuffer == null)
                        return;
                    prevTempBuffer = tempbuffer.CloneArray();
                    currentStepType = type;
                    break;
                case EditingStep.StepType.position:
                    prevTempPos = obj.m_position;
                    currentStepType = type;
                    break;
                case EditingStep.StepType.rotation:
                    prevTempRot = obj.m_rotation;
                    currentStepType = type;
                    break;
                case EditingStep.StepType.moveTo:
                    prevTempRot = obj.m_rotation;
                    prevTempPos = obj.m_position;
                    currentStepType = type;
                    break;
                case EditingStep.StepType.mirrorX:
                case EditingStep.StepType.mirrorY:
                case EditingStep.StepType.mirrorZ:
                    currentStepType = type;
                    break;
            }
        }
        public void InitializeNewStep(EditingStep.StepType type, float stretchFactor) // for stretching
        {
            if (currentStepType != EditingStep.StepType.none) // if already registering an action then don't overlap the other one, just make it one action.
                return;
            currentStepType = type;
            currentStretchFactor = stretchFactor;
        }
        public void ConfirmNewStep(Vertex[] tempbuffer)
        {
            if (stepsUndone.Count > 0)
                stepsUndone.Clear();
            if (stepsDone.Count >= 50)
                stepsDone.RemoveAt(0);
            switch (currentStepType)
            {
                case EditingStep.StepType.none:
                    return;
                case EditingStep.StepType.vertices:
                    if (prevTempBuffer == null)
                        return;
                    if (tempbuffer == null)
                    {
                        prevTempBuffer = null;
                        return;
                    }
                    stepsDone.Add(new EditingStep(prevTempBuffer, tempbuffer));
                    break;
                case EditingStep.StepType.position:
                    var step = new EditingStep(prevTempPos, obj.m_position);
                    step.axisUsed = axisUsed;
                    stepsDone.Add(step);
                    break;
                case EditingStep.StepType.rotation:
                    stepsDone.Add(new EditingStep(prevTempRot, obj.m_rotation));
                    break;
                case EditingStep.StepType.moveTo:
                    stepsDone.Add(new EditingStep(prevTempPos, obj.m_position, prevTempRot, obj.m_rotation));
                    break;
                case EditingStep.StepType.mirrorX:
                case EditingStep.StepType.mirrorY:
                case EditingStep.StepType.mirrorZ:
                    stepsDone.Add(new EditingStep(currentStepType));
                    break;
                case EditingStep.StepType.stretchX:
                case EditingStep.StepType.stretchY:
                case EditingStep.StepType.stretchZ:
                    stepsDone.Add(new EditingStep(currentStepType, currentStretchFactor));
                    break;
            }
            prevTempBuffer = null;
            currentStretchFactor = 1f;
            currentStepType = EditingStep.StepType.none;
            axisUsed = AxisEditionState.none;
        }

        public EditingStep LastStep
        {
            get
            {
                return stepsDone[stepsDone.Count - 1];
            }
        }

        public EditingStep.StepType UndoLastStep(Vertex[] currentTempBuffer, out Vertex[] buffer)
        {
            buffer = currentTempBuffer;
            if (stepsDone == null)
                return EditingStep.StepType.none;
            if (stepsDone.Count == 0)
                return EditingStep.StepType.none;

            var last = LastStep;
            switch (last.type)
            {
                case EditingStep.StepType.vertices:
                    buffer = last.UndoVerticesStep(currentTempBuffer);
                    break;
                case EditingStep.StepType.position:
                    last.UndoPosRotMoveTo(obj);
                    break;
                case EditingStep.StepType.rotation:
                    last.UndoPosRotMoveTo(obj);
                    break;
                case EditingStep.StepType.moveTo:
                    last.UndoPosRotMoveTo(obj);
                    break;
                case EditingStep.StepType.mirrorX:
                    VertexUtils.MirrorX(currentTempBuffer, obj);
                    break;
                case EditingStep.StepType.mirrorY:
                    VertexUtils.MirrorY(currentTempBuffer, obj);
                    break;
                case EditingStep.StepType.mirrorZ:
                    VertexUtils.MirrorZ(currentTempBuffer, obj);
                    break;
                case EditingStep.StepType.stretchX:
                    VertexUtils.StretchX(currentTempBuffer, 1 / last.stretchFactor);
                    break;
                case EditingStep.StepType.stretchY:
                    VertexUtils.StretchY(currentTempBuffer, 1 / last.stretchFactor);
                    break;
                case EditingStep.StepType.stretchZ:
                    VertexUtils.StretchZ(currentTempBuffer, 1 / last.stretchFactor);
                    break;
            }
            if (stepsDone.Contains(last))
                stepsDone.Remove(last);
            stepsUndone.Add(last);
            return last.type;
        }
        public EditingStep.StepType RedoUndoneStep(Vertex[] currentTempBuffer, out Vertex[] buffer)
        {
            buffer = currentTempBuffer;
            if (stepsUndone == null)
                return EditingStep.StepType.none;
            if (stepsUndone.Count == 0)
                return EditingStep.StepType.none;

            var last = stepsUndone[stepsUndone.Count - 1];
            switch (last.type)
            {
                case EditingStep.StepType.vertices:
                    buffer = last.RedoVerticesStep(currentTempBuffer);
                    break;
                case EditingStep.StepType.position:
                    last.RedoPosRotMoveTo(obj);
                    break;
                case EditingStep.StepType.rotation:
                    last.RedoPosRotMoveTo(obj);
                    break;
                case EditingStep.StepType.moveTo:
                    last.RedoPosRotMoveTo(obj);
                    break;
                case EditingStep.StepType.mirrorX:
                    VertexUtils.MirrorX(currentTempBuffer, obj);
                    break;
                case EditingStep.StepType.mirrorY:
                    VertexUtils.MirrorY(currentTempBuffer, obj);
                    break;
                case EditingStep.StepType.mirrorZ:
                    VertexUtils.MirrorZ(currentTempBuffer, obj);
                    break;
                case EditingStep.StepType.stretchX:
                    VertexUtils.StretchX(currentTempBuffer, last.stretchFactor);
                    break;
                case EditingStep.StepType.stretchY:
                    VertexUtils.StretchY(currentTempBuffer, last.stretchFactor);
                    break;
                case EditingStep.StepType.stretchZ:
                    VertexUtils.StretchZ(currentTempBuffer, last.stretchFactor);
                    break;
            }
            if (stepsUndone.Contains(last))
                stepsUndone.Remove(last);
            stepsDone.Add(last);
            return last.type;
        }
    }
}
