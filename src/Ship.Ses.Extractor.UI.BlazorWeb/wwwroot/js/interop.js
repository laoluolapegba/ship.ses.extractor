window.saveAsFile = (filename, content) => {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();

    setTimeout(() => {
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }, 0);
};

// wwwroot/js/mapping-designer.js
window.mappingDesigner = {
    initialize: function (dotnetRef, containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        this.dotnetRef = dotnetRef;
        this.container = container;
        this.setupDragAndDrop();
        this.setupConnectionLines();
    },

    setupDragAndDrop: function () {
        // Make EMR columns draggable
        const emrColumns = this.container.querySelectorAll('.emr-tree-item.column-item');
        emrColumns.forEach(column => {
            column.setAttribute('draggable', 'true');
            column.addEventListener('dragstart', e => {
                e.dataTransfer.setData('application/emr-column', column.dataset.id);
                e.dataTransfer.effectAllowed = 'link';
            });
        });

        // Make FHIR fields droppable
        const fhirFields = this.container.querySelectorAll('.fhir-tree-item');
        fhirFields.forEach(field => {
            if (field.classList.contains('has-children')) return; // Only allow leaf nodes

            field.addEventListener('dragover', e => {
                if (e.dataTransfer.types.includes('application/emr-column')) {
                    e.preventDefault();
                    field.classList.add('drag-over');
                }
            });

            field.addEventListener('dragleave', e => {
                field.classList.remove('drag-over');
            });

            field.addEventListener('drop', e => {
                e.preventDefault();
                field.classList.remove('drag-over');

                const emrColumnId = e.dataTransfer.getData('application/emr-column');
                if (emrColumnId) {
                    this.dotnetRef.invokeMethodAsync('HandleDropMapping', emrColumnId, field.dataset.id);
                }
            });
        });
    },

    setupConnectionLines: function () {
        // This would implement the visual connection lines between mapped elements
        // For a production app, you might use a library like jsPlumb or a custom SVG solution
    },

    refreshConnectionLines: function () {
        // Update the visual connections when mappings change
    },

    highlight: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.classList.add('highlighted');
            setTimeout(() => {
                element.classList.remove('highlighted');
            }, 2000);
        }
    }
};