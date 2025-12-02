const regex = /^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#_-])[A-Za-z\d@$!%*?&.#_-]{8,}$/;

function validarContrasenia() {
    const contra = document.getElementById("Contrasenia").value;
    return regex.test(contra);
}