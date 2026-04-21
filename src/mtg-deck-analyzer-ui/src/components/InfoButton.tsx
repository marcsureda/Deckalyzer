interface InfoButtonProps {
  /** Key into the metric explanations dictionary — used as the aria-label suffix. */
  label: string;
  onClick: () => void;
}

/**
 * Small inline button that triggers a metric-explanation modal.
 * Extracted from PowerOverview to avoid re-creating the component definition on every render.
 */
export default function InfoButton({ label, onClick }: InfoButtonProps) {
  return (
    <button
      className="metric-info-btn"
      onClick={(e) => { e.stopPropagation(); onClick(); }}
      aria-label={`What is ${label}?`}
      title="What does this mean?"
    >
      ℹ
    </button>
  );
}