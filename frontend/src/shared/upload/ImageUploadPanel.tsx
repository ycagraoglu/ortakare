import { useState, type ChangeEvent } from "react";

import { defaultImageUploadPolicy, formatFileSize } from "@/shared/upload/image-upload-policy";
import type { ImageUploadPolicy, UploadProgress } from "@/shared/upload/upload-types";
import { useFilePreview } from "@/shared/upload/use-file-preview";
import { useUploadTask } from "@/shared/upload/use-upload-task";
import { validateImageFile } from "@/shared/upload/validate-image-file";
import "@/shared/upload/upload.css";

type UploadOperation<TResponse> = (
  file: File,
  options: {
    signal: AbortSignal;
    clientUploadId: string;
    onProgress: (progress: UploadProgress) => void;
  },
) => Promise<TResponse>;

type ImageUploadPanelProps<TResponse> = {
  upload: UploadOperation<TResponse>;
  policy?: ImageUploadPolicy;
  onUploaded?: (response: TResponse) => void;
};

export function ImageUploadPanel<TResponse>({
  upload,
  policy = defaultImageUploadPolicy,
  onUploaded,
}: ImageUploadPanelProps<TResponse>) {
  const [file, setFile] = useState<File | null>(null);
  const [selectionError, setSelectionError] = useState<string | null>(null);
  const previewUrl = useFilePreview(file);
  const task = useUploadTask<TResponse>();

  function handleFileChange(event: ChangeEvent<HTMLInputElement>): void {
    const selectedFile = event.target.files?.[0] ?? null;
    task.reset();
    setSelectionError(null);

    if (!selectedFile) {
      setFile(null);
      return;
    }

    const validation = validateImageFile(selectedFile, policy);
    if (!validation.isValid) {
      event.target.value = "";
      setFile(null);
      setSelectionError(validation.message);
      return;
    }

    setFile(selectedFile);
  }

  async function handleUpload(): Promise<void> {
    if (!file || task.status === "uploading") return;

    const result = await task.run((options) => upload(file, options));
    if (result) onUploaded?.(result);
  }

  const isUploading = task.status === "uploading";
  const progressLabel = task.progress.percentage === null
    ? "Yükleniyor…"
    : `%${task.progress.percentage} yüklendi`;

  return (
    <section className="image-upload" aria-labelledby="image-upload-title">
      <div>
        <h2 id="image-upload-title">Fotoğraf yükle</h2>
        <p>JPEG, PNG veya WebP · En fazla {formatFileSize(policy.maxFileSizeBytes)}</p>
      </div>

      <label className="image-upload__picker">
        <span>Dosya seç</span>
        <input
          type="file"
          accept={policy.allowedMimeTypes.join(",")}
          onChange={handleFileChange}
          disabled={isUploading}
        />
      </label>

      {selectionError ? <p className="image-upload__error" role="alert">{selectionError}</p> : null}
      {task.errorMessage ? <p className="image-upload__error" role="alert">{task.errorMessage}</p> : null}

      {file && previewUrl ? (
        <div className="image-upload__selection">
          <img src={previewUrl} alt="Yüklenecek fotoğraf önizlemesi" />
          <div>
            <strong>{file.name}</strong>
            <span>{formatFileSize(file.size)}</span>
          </div>
        </div>
      ) : null}

      {isUploading ? (
        <div className="image-upload__progress" role="status" aria-live="polite">
          <progress max={100} value={task.progress.percentage ?? undefined} />
          <span>{progressLabel}</span>
        </div>
      ) : null}

      {task.status === "success" ? <p className="image-upload__success" role="status">Fotoğraf başarıyla yüklendi.</p> : null}
      {task.status === "cancelled" ? <p role="status">Yükleme iptal edildi.</p> : null}

      <div className="image-upload__actions">
        <button type="button" onClick={() => void handleUpload()} disabled={!file || isUploading}>
          {isUploading ? "Yükleniyor…" : "Yükle"}
        </button>
        {isUploading ? <button type="button" onClick={task.cancel}>İptal et</button> : null}
        {file && !isUploading ? <button type="button" onClick={() => { setFile(null); task.reset(); }}>Temizle</button> : null}
      </div>
    </section>
  );
}
