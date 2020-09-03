using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DevIo.App.ViewModels;
using DevIO.Business.Interfaces;
using AutoMapper;
using DevIO.Business.Models;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace DevIO.App.Controllers
{
    public class ProdutosController : BaseController
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IFornecedorRepository _fornecedorRepository; //Para popularmos a lista de fornecedores, e apresentar no DropDown
        private readonly IMapper _mapper;

        public ProdutosController(IProdutoRepository produtoRepository, IFornecedorRepository fornecedorRepository, IMapper mapper)
        {
            _produtoRepository = produtoRepository;
            _fornecedorRepository = fornecedorRepository;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            return View(_mapper.Map<IEnumerable<ProdutoViewModel>>(await _produtoRepository.ObterProdutosFornecedores()));
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var produtoViewModel = await ObterProduto(id);

            if (produtoViewModel == null) return NotFound();

            return View(produtoViewModel);
        }

        public async Task<IActionResult> Create()
        {
            //Quando vamos criar o produto, temos uma model vazia, então não podemos usar o método ObterProduto pois ele foi obtido através do Id,
            //e nesse caso não temos o id porque vamos CRIAR o PRODUTO, sendoassim criamos outro método privado para fazer esse trabalho.

            var produtoViewModel = await PopularFornecedores(new ProdutoViewModel());
            return View(produtoViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProdutoViewModel produtoViewModel)
        {
            //Primeiro vamos validar a ViewModel, pois se houver algum erro, precisamos devolver ela já com os fornecedores populados para que o usuário possa recriar.

            produtoViewModel = await PopularFornecedores(produtoViewModel);

            if (!ModelState.IsValid) return View(produtoViewModel);

            var imgPrefixo = Guid.NewGuid() + "_";
            if (!await UploadArquivo(produtoViewModel.ImagemUpload, imgPrefixo))
            {
                return View(produtoViewModel);//Se o UploadArquvio não retornar true, retorna com o erro de adicionar a img pro usuário.
            }

            produtoViewModel.Imagem = imgPrefixo + produtoViewModel.ImagemUpload.FileName;

            await _produtoRepository.Adicionar(_mapper.Map<Produto>(produtoViewModel));

            return RedirectToAction("Index");
            //Este é provisório, ou seja, ta certo só que vamos  melhorar mais a frente, implementar upload de imagem etc..
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var produtoViewModel = await ObterProduto(id);

            if (produtoViewModel == null) return NotFound();

            return View(produtoViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ProdutoViewModel produtoViewModel)
        {
            if (id != produtoViewModel.Id) return NotFound();

            var produtoAtualizacao = await  ObterProduto(id); //Obtém o produto que está na Base para preencher a viewModel que retorna pro usuário caso não seja válida.
            produtoViewModel.Fornecedor = produtoAtualizacao.Fornecedor;
            produtoViewModel.Imagem = produtoAtualizacao.Imagem; //Passa os valores que são obrigatórios no retorno da ViewModel, Fornecedor e Imagem

            if (!ModelState.IsValid) return View(produtoViewModel);

            //Agora vamos validar se a imagem foi preenchida.
            if(produtoViewModel.ImagemUpload != null)
            {
                var imgPrefixo = Guid.NewGuid() + "_";
                if (!await UploadArquivo(produtoViewModel.ImagemUpload, imgPrefixo))
                {
                    return View(produtoViewModel);//Se o UploadArquvio não retornar true, retorna com o erro de adicionar a img pro usuário.
                }

                //Caso der certo, preciso salvar a nova imagem no banco, então eu já passso para o produtoAtualizacao.Imagem o nome da 
                //nova imagem criada acima.
                produtoAtualizacao.Imagem = imgPrefixo + produtoViewModel.ImagemUpload.FileName;
            }

            produtoAtualizacao.Nome = produtoViewModel.Nome;
            produtoAtualizacao.Descricao = produtoViewModel.Descricao;
            produtoAtualizacao.Valor = produtoViewModel.Valor;
            produtoAtualizacao.Ativo = produtoViewModel.Ativo;
            //Aqui acima estamos passando pro nosso produto que buscamos no banco, os valores que podem ter sido alterados na ViewModel

            await _produtoRepository.Atualizar(_mapper.Map<Produto>(produtoViewModel));

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var produto = await ObterProduto(id);

            if (produto == null) return NotFound();

            return View(produto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var produto = await ObterProduto(id); //=> indo no banco validar se existe um produto com o id informado

            if (produto == null) return NotFound();

            await _produtoRepository.Remover(id);

            return RedirectToAction("Index");
        }

        private async Task<ProdutoViewModel> ObterProduto(Guid id)
        {
            //obs: vamos popular a lista através de uma consulta no repositório.
            var produto = _mapper.Map<ProdutoViewModel>(await _produtoRepository.ObterProdutoFornecedor(id)); //Nesse método eu já vou ter o produto e o fornecedor DELE!

            //Agora vamos popular a lista de Fornecedores que criamos no ProdutoViewModel
            produto.Fornecedores = _mapper.Map<IEnumerable<FornecedorViewModel>>(await _fornecedorRepository.ObterTodos());

            return produto;
            //Com isto já retornamos o Produto com seu Fornecedor e a lista de fornecedores já preenchida.
        }

        private async Task<ProdutoViewModel> PopularFornecedores(ProdutoViewModel produtoViewModel)
        {
            produtoViewModel.Fornecedores = _mapper.Map<IEnumerable<FornecedorViewModel>>(await _fornecedorRepository.ObterTodos());
            return produtoViewModel;
            //Apartir de qualquer ViewModel que eu passe ele popula os fornecedores daquela ViewModel!
        }

        private async Task<bool> UploadArquivo(IFormFile arquivo, string imgPrefixo)
        {
            if (arquivo.Length <= 0) return false; //Se o tamanho do arquivo for menor = a 0 tem coisa errada ai retorna FALSE

            //Cria nova pasta no wwroot chamada imagens.
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", imgPrefixo + arquivo.FileName);
            //Explicação: Criando um Path fazendo a combinação do meu diretório local + wwroot/imagens que é onde vamos salvar a img, e o arquivo que to querendo subir que seria o prefixo + o fileName!

            //Validação se já existe um arquivo de mesmo nome salvo
            if (System.IO.File.Exists(path))
            {
                ModelState.AddModelError(string.Empty, "Já existe um arquivo com este nome!");
                return false;
            }

            //Usar um FileStream passando o pAth que montamos e o FileMode.Create para criarmos o arquivo.
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream); //Isso pra gravar em disco
            }

            return true;
        }
    }
}
