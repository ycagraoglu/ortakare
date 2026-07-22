import {
  type ReactNode,
  useEffect,
  useId,
  useRef,
} from "react";

import "@/shared/accessibility/dialog.css";

interface AccessibleDialogProps {
  open: boolean;
  title: string;
  description?: string;
  children: ReactNode;
  onClose: () => void;
  closeLabel?: string;
}

export function AccessibleDialog({
  open,
  title,
  description,
  children,
  onClose,
  closeLabel = "Kapat",
}: AccessibleDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const triggerRef = useRef<HTMLElement | null>(null);
  const titleId = useId();
  const descriptionId = useId();

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open && !dialog.open) {
      triggerRef.current = document.activeElement as HTMLElement | null;
      dialog.showModal();
      return;
    }

    if (!open && dialog.open) {
      dialog.close();
      triggerRef.current?.focus();
      triggerRef.current = null;
    }
  }, [open]);

  useEffect(() => {
    return () => {
      triggerRef.current?.focus();
    };
  }, []);

  return (
    <dialog
      ref={dialogRef}
      className="accessible-dialog"
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClose={() => {
        if (open) onClose();
        triggerRef.current?.focus();
        triggerRef.current = null;
      }}
    >
      <div className="accessible-dialog__header">
        <div>
          <h2 id={titleId}>{title}</h2>
          {description ? <p id={descriptionId}>{description}</p> : null}
        </div>
        <button type="button" onClick={onClose} aria-label={closeLabel}>
          ×
        </button>
      </div>
      <div className="accessible-dialog__body">{children}</div>
    </dialog>
  );
}
