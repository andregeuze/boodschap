window.groceryList = {
  initDragHandleReorder: function (selector, handleSelector, dotNetRef) {
    const rows = document.querySelectorAll(selector);
    const dragSourceClasses = ["opacity-80", "scale-[1.01]", "shadow-lg", "z-10", "bg-slate-100", "border-slate-400"];
    const dropTargetClasses = ["ring-2", "ring-slate-500", "bg-slate-100", "border-slate-400"];

    rows.forEach((row) => {
      if (row.dataset.dragHandleInit === "1") {
        return;
      }

      row.dataset.dragHandleInit = "1";

      const handle = row.querySelector(handleSelector);
      if (!handle) {
        return;
      }

      let dragActive = false;
      let sourceIndex = -1;
      let targetIndex = -1;
      let capturedPointerId = -1;
      let highlightedTarget = null;

      const clearHighlight = () => {
        if (highlightedTarget) {
          highlightedTarget.classList.remove(...dropTargetClasses);
          highlightedTarget = null;
        }
      };

      const endDrag = async () => {
        if (capturedPointerId >= 0) {
          try { handle.releasePointerCapture(capturedPointerId); } catch {}
          capturedPointerId = -1;
        }

        row.classList.remove(...dragSourceClasses);
        document.body.classList.remove("cursor-grabbing", "select-none");

        const localSource = sourceIndex;
        const localTarget = targetIndex;
        dragActive = false;
        sourceIndex = -1;
        targetIndex = -1;
        clearHighlight();

        if (localSource >= 0 && localTarget >= 0 && localSource !== localTarget) {
          try {
            await dotNetRef.invokeMethodAsync("ReorderItem", localSource, localTarget);
          } catch {}
        }
      };

      handle.addEventListener("pointerdown", (event) => {
        event.preventDefault();
        event.stopPropagation();

        const parsedIndex = Number.parseInt(row.dataset.index ?? "-1", 10);
        if (!Number.isFinite(parsedIndex) || parsedIndex < 0) {
          return;
        }

        sourceIndex = parsedIndex;
        targetIndex = parsedIndex;
        dragActive = true;
        capturedPointerId = event.pointerId;
        handle.setPointerCapture(event.pointerId);

        row.classList.add(...dragSourceClasses);
        document.body.classList.add("cursor-grabbing", "select-none");
      });

      handle.addEventListener("pointermove", (event) => {
        if (!dragActive) {
          return;
        }

        // Use midpoint Y of each row to find drop target — reliable even with z-index/scale transforms
        const allRows = Array.from(document.querySelectorAll(selector));
        let closestRow = null;
        let closestDist = Infinity;
        let closestIndex = -1;

        allRows.forEach((r) => {
          const rect = r.getBoundingClientRect();
          const midY = rect.top + rect.height / 2;
          const dist = Math.abs(event.clientY - midY);
          if (dist < closestDist) {
            closestDist = dist;
            closestRow = r;
            const idx = Number.parseInt(r.dataset.index ?? "-1", 10);
            closestIndex = Number.isFinite(idx) && idx >= 0 ? idx : -1;
          }
        });

        if (!closestRow || closestIndex < 0) {
          return;
        }

        targetIndex = closestIndex;
        if (highlightedTarget !== closestRow) {
          clearHighlight();
          if (closestRow !== row) {
            highlightedTarget = closestRow;
            highlightedTarget.classList.add(...dropTargetClasses);
          }
        }
      });

      handle.addEventListener("pointerup", () => { endDrag(); });
      handle.addEventListener("pointercancel", () => { endDrag(); });
    });
  },

  dispose: function (selector) {
    const rows = document.querySelectorAll(selector);
    rows.forEach((row) => {
      row.dataset.dragHandleInit = "0";
      row.classList.remove(
        "opacity-80", "scale-[1.01]", "shadow-lg", "z-10",
        "bg-slate-100", "border-slate-400", "ring-2", "ring-slate-500"
      );
    });
    document.body.classList.remove("cursor-grabbing", "select-none");
  }
};
