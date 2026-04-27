interface Props {
  title: string;
  message: string;
  confirmLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ConfirmModal({ title, message, confirmLabel = "Continue", onConfirm, onCancel }: Props) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-md rounded-xl bg-gray-900 border border-gray-700 shadow-2xl p-6 flex flex-col gap-4">
        <h2 className="text-lg font-semibold text-gray-100">{title}</h2>
        <p className="text-sm text-gray-400 leading-relaxed whitespace-pre-line">{message}</p>
        <div className="flex justify-end gap-3 pt-2">
          <button
            onClick={onCancel}
            className="rounded-lg px-5 py-2 text-sm font-medium text-gray-400 hover:text-gray-200 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            className="rounded-lg bg-purple-700 hover:bg-purple-600 px-5 py-2 text-sm font-semibold transition-colors"
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
