using System.Text;

namespace RimMind.Application.Features.Prompt
{
    internal sealed class TaskInstructionBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public TaskInstructionBuilder AddRole(string role)
        {
            _sb.AppendLine($"You are {role}.");
            return this;
        }

        public TaskInstructionBuilder AddObjective(string objective)
        {
            _sb.AppendLine($"Objective: {objective}");
            return this;
        }

        public TaskInstructionBuilder AddConstraint(string constraint)
        {
            _sb.AppendLine($"Constraint: {constraint}");
            return this;
        }

        public TaskInstructionBuilder AddContext(string context)
        {
            _sb.AppendLine($"Context: {context}");
            return this;
        }

        public TaskInstructionBuilder AddFormat(string format)
        {
            _sb.AppendLine($"Response format: {format}");
            return this;
        }

        public string Build()
        {
            return _sb.ToString();
        }

        public void Reset() => _sb.Clear();
    }
}
