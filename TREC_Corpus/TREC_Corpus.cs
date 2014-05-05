using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections;
using nsText_Analysis;
using nsPhrase_Detection;
using nsWord_Sense_Disambiguation;

namespace nsTREC_Corpus
{
    public class TREC_Corpus
    {
        string corpus_path;
        char format_type; //P or T
        Encoding corpus_encoding;

        StreamReader corpus_reader;
        string current_document;
        string line = string.Empty;


        public TREC_Corpus(string path, char type, Encoding encoding)
        {
            corpus_path = path;
            format_type = type;
            corpus_encoding = encoding;
        }

        public TREC_Corpus(string path, char type, int codepage)
        {
            corpus_path = path;
            format_type = type;
            corpus_encoding = Encoding.GetEncoding(codepage);
        }

        public bool get_first_DOC(out string document)
        {
            try
            {
                //this function is used for transit the pointer to the beginning of the corpus
                try_detect_format();
                return get_next_DOC(out document);
            }
            catch (Exception exc)
            {
                document = null;
                throw new Exception(exc.Message, exc);
            }
        }

        public bool get_next_DOC(out string document)
        {
            if (format_type == 'T')
                return get_next_DOC_TREC_format(out document);
            else if (format_type == 'P')
                return get_next_DOC_PTF_format(out document);
            else
            { document = null; return false; }
        }

        bool get_next_DOC_TREC_format(out string document)
        {
            //XmlDocument xmldoc = new XmlDocument();
            StringBuilder document_str = new StringBuilder();
            document_str.Remove(0, document_str.Length);
            try
            {
                do
                {
                    if (line.Contains("<DOC>"))
                    {
                        document_str.AppendLine(line.Substring(line.IndexOf("<DOC>")));
                        while (!(line = corpus_reader.ReadLine()).Contains("</DOC>"))
                        {
                            document_str.AppendLine(line);
                        }
                        document_str.AppendLine(line.Substring(0, line.IndexOf("</DOC>") + 6));
                        break;
                    }
                } while ((line = corpus_reader.ReadLine()) != null);

                if (line == null)
                {
                    document = null;
                    current_document = null;
                    corpus_reader.Close();
                    return false;
                }
                else
                {
                    document_str.Replace("&HT;", "");
                    document_str.Replace("&QC;", "");
                    document_str.Replace("&LR;", "");
                    document_str.Replace("&AMP;", "");
                    document_str.Replace("&QL;", "");
                    document_str.Replace("&UR;", "");
                    document_str.Replace("&QR;", "");
                    document_str.Replace("&Cx17;", "");
                    document_str.Replace("&Cx0b;", "");
                    document_str.Replace("&Cx0e;", "");

                    current_document = document = document_str.ToString();
                    return true;
                }
            }
            catch (Exception readEx)
            {
                document = null;
                throw new Exception(readEx.Message, readEx);
            }
        }

        bool get_next_DOC_PTF_format(out string document)
        {
            StringBuilder document_str = new StringBuilder();
            document_str.Remove(0, document_str.Length);
            try
            {
                do
                {
                    if (line.Contains("<DOC "))
                    {
                        document_str.AppendLine(line.Substring(line.IndexOf("<DOC ")));
                        while (!(line = corpus_reader.ReadLine()).Contains("</DOC>"))
                        {
                            document_str.AppendLine(line);
                        }
                        document_str.AppendLine(line.Substring(0, line.IndexOf("</DOC>") + 6));
                        break;
                    }
                } while ((line = corpus_reader.ReadLine()) != null);

                if (line == null)
                {
                    document = null;
                    current_document = null;
                    corpus_reader.Close();
                    return false;
                }
                else
                {
                    current_document = document = document_str.ToString();
                    return true;
                }
            }
            catch (Exception readEx)
            {
                document = null;
                throw new Exception(readEx.Message, readEx);
            }
        }

        public void remove_footer_element(ref XmlElement document)
        {
            XmlNodeList footers = document.GetElementsByTagName("FOOTER");

            if (footers.Count > 0)
            {
                if (footers.Count > 1)
                    throw new Exception("More than 1 footer");

                for (int i = 0; i < footers.Count; i++)
                    footers[i].ParentNode.RemoveChild(footers[i]);
            }
        }

        public void alter_header_text(ref XmlElement document)
        {
            XmlNodeList headers = document.GetElementsByTagName("HEADER");

            if (headers.Count > 0)
            {
                if (headers.Count > 1)
                {
                    throw new Exception("More than 1 HEADER");
                }

                for (int i = 0; i < headers.Count; i++)
                {
                    int indexOf_AFP = headers[i].ChildNodes[0].Value.IndexOf("/ÇÝÈ-");
                    if (indexOf_AFP > -1)
                    {
                        string temp = headers[i].ChildNodes[0].Value.Substring(indexOf_AFP);
                        headers[i].ChildNodes[0].Value = temp.Substring(temp.IndexOf(" ") + 1);
                    }
                }
            }
        }

        public void alter_footer_element(ref XmlElement document)
        {
            XmlNodeList footers = document.GetElementsByTagName("FOOTER");

            if (footers.Count > 0)
            {
                if (footers.Count > 1)
                    throw new Exception("More than 1 footer");

                for (int i = 0; i < footers.Count; i++)
                {
                    int index = footers[i].ChildNodes[0].Value.IndexOf((char)1600);
                    string new_value = footers[i].ChildNodes[0].Value.Substring(0, footers[i].ChildNodes[0].Value.IndexOf((char)1600));
                    footers[i].ChildNodes[0].Value = new_value;
                    //footers[i].ParentNode.RemoveChild(footers[i]);
                }
            }
        }

        public void remove_trailer_element(ref XmlElement document)
        {
            XmlNodeList trailers = document.GetElementsByTagName("TRAILER");

            if (trailers.Count > 0)
            {
                if (trailers.Count > 1)
                    throw new Exception("More than 1 Trailer");

                for (int i = 0; i < trailers.Count; i++)
                    trailers[i].ParentNode.RemoveChild(trailers[i]);
            }
        }

        public void from_PTF_format_to_TREC_format(string target_name)
        {
            if (format_type != 'P')
                return;
            //PTF format: <DOC 1></DOC>
            //            <DOC 2></DOC>

            //<DOC>
            //<DOCNO>document_id</DOCNO>
            //<TEXT>Index this document text.
            //</TEXT>
            //</DOC>
            string inPath = corpus_path;
            string outPath = Path.GetDirectoryName(inPath) + "\\" + target_name;

            string word = string.Empty;
            string stem = string.Empty;

            using (StreamReader sr = new StreamReader(inPath, corpus_encoding))
            {
                using (StreamWriter sw = new StreamWriter(outPath, false, corpus_encoding))
                {
                    while ((word = sr.ReadLine()) != null)
                    {
                        if (word.StartsWith("<DOC "))
                        {
                            sw.WriteLine("<DOC>");
                            sw.WriteLine("<DOCNO>" + word.Substring(5, word.Length - 6) + "</DOCNO>");
                            sw.WriteLine("<TEXT>");
                        }
                        else
                            if (word.Contains("</DOC>"))
                            {
                                sw.WriteLine("</TEXT>");
                                sw.WriteLine(word);
                            }
                            else
                            {
                                if (word != null && word.CompareTo("") != 0)
                                    sw.WriteLine(word);
                            }
                    }
                }
            }

        }

        void from_TREC_format_to_PTF_format(string target_name)
        {
            if (format_type != 'T')
                return;

            string inPath = corpus_path;
            string outPath = Path.GetDirectoryName(inPath) + "\\" + target_name;


            string word = string.Empty;
            string stem = string.Empty;

            using (StreamReader sr = new StreamReader(inPath, corpus_encoding))
            {
                using (StreamWriter sw = new StreamWriter(outPath, false, corpus_encoding))
                {
                    while ((word = sr.ReadLine()) != null)
                    {
                        if (word.Contains("<DOC>"))
                        {
                            sw.WriteLine("<DOC>");
                            sw.WriteLine("<DOCNO>" + word.Substring(5, word.Length - 6) + "</DOCNO>");
                            sw.WriteLine("<TEXT>");
                        }
                        else
                            if (word.Contains("</DOC>"))
                            {
                                sw.WriteLine("</TEXT>");
                                sw.WriteLine(word);
                            }
                            else
                            {
                                if (word != null && word.CompareTo("") != 0)
                                    sw.WriteLine(stem);
                            }
                    }
                }
            }
        }

        public void convert_the_topics_into_LemurQueryFormat()
        {
            string topics_file_path = "C:\\D\\Work\\Lib\\Dos Digital Library (Manual)\\( Codes\\resources\\Test Collections & Evaluation Tools\\LDC2001T55\\TOPICS\\encoded Arabic\\encodedArabicTopicsTREC2002.txt";
            string[] separator = { "<top>", "</top>" };
            string[] separator2 = { "<title>", "<desc> Description:", "<narr> Narrative:" };
            using (StreamReader sr = new StreamReader(topics_file_path, corpus_encoding))
            {
                string file_content = sr.ReadToEnd();
                file_content = file_content.Replace("\n", string.Empty);
                string[] topics = file_content.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                using (StreamWriter sw = new StreamWriter("C:\\D\\Work\\Goals\\Academic\\Master\\Technical Reports\\Report 05 - Information Retrieval in context of Arabic Language\\New Folder\\Refined_lemur_queries_TDN.txt", true, corpus_encoding))
                {
                    int counter = 0;
                    for (int i = 1; i <= topics.Length; i++)
                    {
                        if (topics[i - 1].Contains("<num>"))
                        {
                            sw.WriteLine("<DOC><DOCNO>" + (int)(++counter + 25) + "</DOCNO><TEXT>");

                            string[] TDN = topics[i - 1].Split(separator2, StringSplitOptions.RemoveEmptyEntries);
                            sw.WriteLine(TDN[1]);
                            sw.WriteLine(TDN[2]);
                            sw.WriteLine(TDN[3]);
                            sw.WriteLine("</TEXT></DOC>");
                        }
                    }
                }
            }

        }
        //
        //return:
        //format string
        public char try_detect_format()
        {
            if (corpus_reader != null)
                corpus_reader.Close();

            corpus_reader = new StreamReader(corpus_path, corpus_encoding);

            while (!(line = corpus_reader.ReadLine()).Contains("<DOC") && line != null) ;

            if (line != null)
                if (line.Contains("<DOC>"))
                   return format_type = 'T';
                else if (line.Contains("<DOC ")) 
                    return format_type = 'P';

            return format_type = 'U';
        }

        public void put_all_documents_in_one_file()
        {
            ArrayList AllfilePathes = new ArrayList();
            string[] paths = Directory.GetDirectories("C:\\D\\Work\\Lib\\Dos Digital Library (Manual)\\( Codes\\resources\\Test Collections & Evaluation Tools\\LDC2001T55\\TRANSCRIPTS\\encoded");
            foreach (string path in paths)
            {
                //string[] filePathes = Directory.GetFiles(path);
                AllfilePathes.AddRange(Directory.GetFiles(path));
            }

            using (StreamWriter sw = new StreamWriter("C:\\D\\Work\\Lib\\Dos Digital Library (Manual)\\( Codes\\resources\\Test Collections & Evaluation Tools\\LDC2001T55\\TRANSCRIPTS\\encoded\\AllDocs.txt", false, corpus_encoding))
            {
                string line = string.Empty;
                foreach (string filePath in AllfilePathes)
                    using (StreamReader sr = new StreamReader(filePath, corpus_encoding))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            //if (line.StartsWith("<html"))
                            //    sw.WriteLine("<DOC>");
                            //else if (line.EndsWith("html>"))
                            //{ }
                            //else
                            sw.WriteLine(line);
                        }
                    }
            }
        }

        public void make_the_collection_in_One_File()
        {
            string[] separator = { "<html>", "</html>" };
            string[] filesPaths = Directory.GetFiles("C:\\D\\Work\\Lib\\Dos Digital Library (Manual)\\( Codes\\resources\\Test Collections & Evaluation Tools\\LDC2001T55\\TRANSCRIPTS\\encoded", "*.sgm", SearchOption.AllDirectories);
            foreach (string path in filesPaths)
                using (StreamReader sr = new StreamReader(path, corpus_encoding))
                {
                    string[] fileData = sr.ReadToEnd().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    //System.Console.Out.
                    //using (StreamWriter sw = ;
                    File.AppendAllText("C:\\AllData.sgm", fileData[0], corpus_encoding);
                    //{
                    // sw.Write(fileData[0]);
                    //}

                    //MessageBox.Show(fileData[0]);

                }
        }

        public void process_PTF_topics_text(string target_name)
        {
            string word = string.Empty;
            StringBuilder doc_txt;
            Dictionary<int, n_gram> ngrams_dictionary = new Dictionary<int, n_gram>();
            string destination_path = Path.GetDirectoryName(corpus_path) + "\\" + target_name;
            Link_detector ld_obj;

            using (StreamReader sr = new StreamReader(corpus_path, corpus_encoding))
            {
                if (File.Exists(destination_path))
                    destination_path = generate_new_name(Path.GetDirectoryName(corpus_path), target_name);
          
                using (StreamWriter sw = new StreamWriter(destination_path, false, corpus_encoding))
                        while ((word = sr.ReadLine()) != null)
                            if (word.StartsWith("<DOC "))
                            {
                                ld_obj = new Link_detector();
                                ngrams_dictionary.Clear();
                                doc_txt = new StringBuilder();
                                
                                sw.WriteLine(word);
                                while (!(word = sr.ReadLine()).Contains("</DOC>"))
                                    doc_txt.AppendLine(word);

                                ld_obj.wikify(doc_txt.ToString(), ref ngrams_dictionary);

                                //text_analysis.text_processing(ref tokens);
                                foreach (KeyValuePair<int, n_gram> kvp in ngrams_dictionary)
                                {
                                    if (kvp.Value.concepts != null)
                                        foreach (string c in kvp.Value.concepts)
                                            sw.Write(c + ",");

                                    sw.Write(kvp.Value.ngram);
                                    sw.WriteLine();                                    
                                }

                                sw.WriteLine("</DOC>");
                            }
            }
        }

        public void process_corpus(string target_name)
        {
            string destination_path = Path.GetDirectoryName(corpus_path) + "\\" + target_name;
            if (File.Exists(destination_path))
                destination_path = generate_new_name(Path.GetDirectoryName(corpus_path), target_name);
            string[] sep = { "\r\n" };
            text_analysis ta_obj = new text_analysis();
            ta_obj.Analysis_type = 3;
            using (StreamWriter sw = new StreamWriter(destination_path, false, corpus_encoding))
            {
                string document;
                string word;
                if (get_first_DOC(out document))
                {
                   do
                   {
                       List<string> tokens = new List<string>(document.Split(sep, StringSplitOptions.RemoveEmptyEntries));

                       if (tokens[0].StartsWith("<DOC") && tokens[tokens.Count - 1] == "</DOC>")
                       {
                           sw.WriteLine(tokens[0]);
                           tokens.RemoveAt(tokens.Count - 1);
                           tokens.RemoveAt(0);
                           foreach (string token in tokens)
                           {
                               word = ta_obj.text_processing(token, true);

                               if (!string.IsNullOrEmpty(word))
                                   sw.WriteLine(word);
                           }
                           sw.WriteLine("</DOC>");
                       }
                       else
                       {
                           throw new Exception();
                       }
                   } while (get_next_DOC(out document));
                }
            }
        }

        static string generate_new_name(string dir, string file_name)
        {
            int counter = 1;
            string ext = Path.GetExtension(file_name);
            string newName = dir + "\\" + Path.GetFileNameWithoutExtension(file_name) + "(" + counter.ToString() + ")" + ext;

            while (File.Exists(newName))
            {
                counter++;
                newName = dir + "\\" + Path.GetFileNameWithoutExtension(file_name) + "(" + counter.ToString() + ")." + ext;
            }

            return newName;
        }

        public void process_PTF_Documents_text(string target_name)
        {
            string word = string.Empty;
            StringBuilder doc_txt;
            Dictionary<int, n_gram> ngrams_dictionary = new Dictionary<int, n_gram>();
            string destination_path = Path.GetDirectoryName(corpus_path) + "\\" + target_name;
            Link_detector ld_obj;

            using (StreamReader sr = new StreamReader(corpus_path, corpus_encoding))
            {
                if (File.Exists(destination_path))
                    destination_path = generate_new_name(Path.GetDirectoryName(corpus_path), target_name);

                using (StreamWriter sw = new StreamWriter(destination_path, false, corpus_encoding))
                    while ((word = sr.ReadLine()) != null)
                        if (word.StartsWith("<DOC "))
                        {
                            ld_obj = new Link_detector();
                            ngrams_dictionary.Clear();
                            doc_txt = new StringBuilder();

                            sw.WriteLine(word);

                            while (!(word = sr.ReadLine()).Contains("</DOC>"))
                                doc_txt.AppendLine(word);

                            ld_obj.wikify(doc_txt.ToString(), ref ngrams_dictionary);

                            foreach (KeyValuePair<int, n_gram> kvp in ngrams_dictionary)
                            {
                                if (kvp.Value.concepts != null && !string.IsNullOrEmpty(kvp.Value.concept))
                                    sw.WriteLine(kvp.Value.concept);
                                else sw.WriteLine(kvp.Value.ngram);
                            }

                            sw.WriteLine("</DOC>");
                        }
            }
        }
    }
}
