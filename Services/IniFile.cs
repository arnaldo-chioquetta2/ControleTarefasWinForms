using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ControleTarefasWinForms.Services
{
    /// <summary>
    /// Classe helper para ler e gravar dados em arquivos INI usando a API do Windows
    /// </summary>
    public class IniFile
    {
        private readonly string _filePath;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// Construtor que recebe o caminho do arquivo INI
        /// </summary>
        /// <param name="filePath">Caminho completo do arquivo INI</param>
        public IniFile(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Lê um valor do arquivo INI
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <param name="defaultValue">Valor padrão caso não encontre</param>
        /// <returns>Valor lido ou valor padrão</returns>
        public string Read(string section, string key, string defaultValue = "")
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, retVal, 255, _filePath);
            return retVal.ToString();
        }

        /// <summary>
        /// Escreve um valor no arquivo INI
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <param name="value">Valor a ser gravado</param>
        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _filePath);
        }

        /// <summary>
        /// Deleta uma seção inteira do arquivo INI
        /// </summary>
        /// <param name="section">Nome da seção a ser deletada</param>
        public void DeleteSection(string section)
        {
            WritePrivateProfileString(section, null, null, _filePath);
        }

        /// <summary>
        /// Deleta uma chave específica de uma seção
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave a ser deletada</param>
        public void DeleteKey(string section, string key)
        {
            WritePrivateProfileString(section, key, null, _filePath);
        }

        /// <summary>
        /// Verifica se o arquivo INI existe
        /// </summary>
        public bool FileExists()
        {
            return File.Exists(_filePath);
        }
    }
}
